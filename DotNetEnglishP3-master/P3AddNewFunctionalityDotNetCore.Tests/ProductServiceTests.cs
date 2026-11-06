using Microsoft.Extensions.Localization;
using Moq;
using P3AddNewFunctionalityDotNetCore.Models;
using P3AddNewFunctionalityDotNetCore.Models.Repositories;
using P3AddNewFunctionalityDotNetCore.Models.Services;
using P3AddNewFunctionalityDotNetCore.Models.ViewModels;
using Xunit;
using System.Globalization;

namespace P3AddNewFunctionalityDotNetCore.Tests
{
    public class ProductServiceTests
    {
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            var cart = new Mock<ICart>();
            var productRepository = new Mock<IProductRepository>();
            var orderRepository = new Mock<IOrderRepository>();
            var localizer = new Mock<IStringLocalizer<ProductService>>();

            localizer
                .Setup(l => l[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            _productService = new ProductService(
                cart.Object,
                productRepository.Object,
                orderRepository.Object,
                localizer.Object);
        }

        #region Name validation tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckProductModelErrors_WhenNameIsMissing_ReturnsMissingName(
            string name)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = name,
                Price = "12",
                Stock = "2"
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Single(errors);
            Assert.Contains("MissingName", errors);
        }

        #endregion

        #region Product validation tests

        [Fact]
        public void CheckProductModelErrors_WhenProductIsValid_ReturnsNoErrors()
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = "12",
                Stock = "2"
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Empty(errors);
        }

        #endregion

        #region Price validation tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckProductModelErrors_WhenPriceIsMissing_ReturnsMissingPrice(
            string price)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = price,
                Stock = "2"
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Single(errors);
            Assert.Contains("MissingPrice", errors);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("12abc")]
        public void CheckProductModelErrors_WhenPriceIsNotNumeric_ReturnsPriceNotANumber(
            string price)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = price,
                Stock = "2"
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Single(errors);
            Assert.Contains("PriceNotANumber", errors);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-12")]
        public void CheckProductModelErrors_WhenPriceIsNotPositive_ReturnsPriceNotGreaterThanZero(
            string price)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = price,
                Stock = "2"
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Single(errors);
            Assert.Contains("PriceNotGreaterThanZero", errors);
        }

        [Theory]
        [InlineData("fr-FR", "12,50")]
        [InlineData("en-US", "12.50")]
        [InlineData("es-ES", "12,50")]
        public void CheckProductModelErrors_WhenPriceIsPositiveDecimal_ReturnsNoErrors
        (string cultureName, string price)
        {
            // Arrange
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);

                var product = new ProductViewModel
                {
                    Name = "Produit test",
                    Price = price,
                    Stock = "2"
                };

                // Act
                var errors = _productService.CheckProductModelErrors(product);

                // Assert
                Assert.Empty(errors);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        public void CheckProductModelErrors_WhenPriceIsNotFinite_ReturnsPriceNotANumber
            (string price)
        {
            // Arrange
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                var product = new ProductViewModel
                {
                    Name = "Produit test",
                    Price = price,
                    Stock = "2"
                };

                // Act
                var errors = _productService.CheckProductModelErrors(product);

                // Assert
                Assert.Single(errors);
                Assert.Contains("PriceNotANumber", errors);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        #endregion

        #region Stock validation tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckProductModelErrors_WhenStockIsMissing_ReturnsMissingStock(
            string stock)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = "12",
                Stock = stock
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Single(errors);
            Assert.Contains("MissingStock", errors);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("1.5")]
        [InlineData("1,5")]
        public void CheckProductModelErrors_WhenStockIsNotInteger_ReturnsStockNotAnInteger(
            string stock)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = "12",
                Stock = stock
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Single(errors);
            Assert.Contains("StockNotAnInteger", errors);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-2")]
        public void CheckProductModelErrors_WhenStockIsNotPositive_ReturnsStockNotGreaterThanZero(
            string stock)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = "12",
                Stock = stock
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Single(errors);
            Assert.Contains("StockNotGreaterThanZero", errors);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("20")]
        public void CheckProductModelErrors_WhenStockIsPositiveInteger_ReturnsNoErrors(
            string stock)
        {
            // Arrange
            var product = new ProductViewModel
            {
                Name = "Produit test",
                Price = "12",
                Stock = stock
            };

            // Act
            var errors = _productService.CheckProductModelErrors(product);

            // Assert
            Assert.Empty(errors);
        }

        #endregion


    }
}