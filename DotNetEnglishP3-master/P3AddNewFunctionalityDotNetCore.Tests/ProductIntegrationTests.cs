using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using P3AddNewFunctionalityDotNetCore.Controllers;
using P3AddNewFunctionalityDotNetCore.Data;
using P3AddNewFunctionalityDotNetCore.Models;
using P3AddNewFunctionalityDotNetCore.Models.Repositories;
using P3AddNewFunctionalityDotNetCore.Models.Services;
using P3AddNewFunctionalityDotNetCore.Models.ViewModels;
using Xunit;

namespace P3AddNewFunctionalityDotNetCore.Tests
{
    // Le repository possède un contexte static :
    // cette collection ne doit pas être exécutée en parallèle.
    [CollectionDefinition("Product integration", DisableParallelization = true)]
    public class ProductIntegrationCollection
    {
    }

    [Collection("Product integration")]
    public class ProductIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly P3Referential _context;
        private readonly Cart _cart;
        private readonly ProductController _controller;
        private readonly CultureInfo _previousCulture;

        #region Initialization and cleanup

        public ProductIntegrationTests()
        {
            _previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // La base existe uniquement tant que cette connexion reste ouverte.
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<P3Referential>()
                .UseSqlite(_connection)
                .Options;

            // Requis par le constructeur de P3Referential.
            // Cette connexion SQL Server n'est pas ouverte par ces tests.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:P3Referential"] =
                        "Server=localhost;Database=P3IntegrationTests;"
                        + "Integrated Security=True;"
                })
                .Build();

            _context = new P3Referential(options, configuration);
            _context.Database.EnsureCreated();

            _cart = new Cart();
            var productRepository = new ProductRepository(_context);
            var orderRepository = new Mock<IOrderRepository>();
            var languageService = new Mock<ILanguageService>();
            var localizer = new Mock<IStringLocalizer<ProductService>>();

            localizer
                .Setup(l => l[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            var productService = new ProductService(
                _cart,
                productRepository,
                orderRepository.Object,
                localizer.Object);

            _controller = new ProductController(
                productService,
                languageService.Object);
        }

        public void Dispose()
        {
            try
            {
                _context.Dispose();
                _connection.Dispose();
            }
            finally
            {
                CultureInfo.CurrentCulture = _previousCulture;
            }
        }

        #endregion

        #region Product creation tests

        [Fact]
        public void Create_WhenProductIsValid_SavesProductAndDisplaysItInCatalog()
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "TEST-INTEGRATION-CREATE",
                Price = "12.50",
                Stock = "2",
                Description = "Produit temporaire de test",
                Details = "Test de creation"
            };

            // Act
            var result = _controller.Create(product);

            // Assert : redirection après une création valide
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Admin", redirect.ActionName);

            // Relire les données enregistrées, sans réutiliser le suivi EF.
            _context.ChangeTracker.Clear();

            var savedProduct = Assert.Single(
                _context.Product.AsNoTracking().ToList());

            Assert.True(savedProduct.Id > 0);
            Assert.Equal(product.Name, savedProduct.Name);
            Assert.Equal(12.5, savedProduct.Price);
            Assert.Equal(2, savedProduct.Quantity);
            Assert.Equal(product.Description, savedProduct.Description);
            Assert.Equal(product.Details, savedProduct.Details);

            // Le catalogue client doit contenir les mêmes informations.
            var catalogResult = Assert.IsType<ViewResult>(
                _controller.Index());

            var catalog = Assert.IsAssignableFrom<
                IEnumerable<ProductViewModel>>(catalogResult.Model);

            var displayedProduct = Assert.Single(catalog);

            Assert.Equal(savedProduct.Id, displayedProduct.Id);
            Assert.Equal(product.Name, displayedProduct.Name);
            Assert.Equal("12.5", displayedProduct.Price);
            Assert.Equal("2", displayedProduct.Stock);
            Assert.Equal(product.Description, displayedProduct.Description);
            Assert.Equal(product.Details, displayedProduct.Details);
        }

        [Fact]
        public void Create_WhenProductIsInvalid_ReturnsErrorsWithoutSavingProduct()
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "TEST-INTEGRATION-INVALID",
                Price = "abc",
                Stock = "0"
            };

            // Act
            var result = _controller.Create(product);

            // Assert : retour au formulaire avec le produit saisi
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(product, view.Model);
            Assert.False(_controller.ModelState.IsValid);

            var errors = _controller.ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .ToList();

            Assert.Equal(2, errors.Count);
            Assert.Contains("PriceNotANumber", errors);
            Assert.Contains("StockNotGreaterThanZero", errors);

            // Aucun enregistrement en base
            _context.ChangeTracker.Clear();
            Assert.Empty(_context.Product.AsNoTracking().ToList());

            // Aucun produit dans le catalogue client
            var catalogResult = Assert.IsType<ViewResult>(
                _controller.Index());

            var catalog = Assert.IsAssignableFrom<
                IEnumerable<ProductViewModel>>(catalogResult.Model);

            Assert.Empty(catalog);
        }

        #endregion

        #region Product deletion tests

        [Fact]
        public void DeleteProduct_RemovesProductFromDatabaseCatalogAndCart()
        {
            // Arrange
            _controller.Create(new ProductViewModel
            {
                Name = "TEST-INTEGRATION-DELETE",
                Price = "12.50",
                Stock = "2"
            });

            _context.ChangeTracker.Clear();

            var savedProduct = _context.Product.Single();
            _cart.AddItem(savedProduct, 1);

            Assert.Single(_cart.Lines);

            // Act
            var result = _controller.DeleteProduct(savedProduct.Id);

            // Assert : redirection après suppression
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Admin", redirect.ActionName);

            // Suppression persistée en base
            _context.ChangeTracker.Clear();
            Assert.Empty(_context.Product.AsNoTracking().ToList());

            // Produit retiré du catalogue
            var catalogResult = Assert.IsType<ViewResult>(
                _controller.Index());

            var catalog = Assert.IsAssignableFrom<
                IEnumerable<ProductViewModel>>(catalogResult.Model);

            Assert.Empty(catalog);

            // Produit également retiré du panier existant
            Assert.Empty(_cart.Lines);
            Assert.Equal(0d, _cart.GetTotalValue());
        }

        #endregion
    }
}