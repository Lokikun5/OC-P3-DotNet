using Microsoft.EntityFrameworkCore;
using P3AddNewFunctionalityDotNetCore.Data;
using P3AddNewFunctionalityDotNetCore.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Data;
using System.Linq;
using P3AddNewFunctionalityDotNetCore.Models.Exceptions;

namespace P3AddNewFunctionalityDotNetCore.Models.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly P3Referential _context;

        public OrderRepository(P3Referential context)
        {
            _context = context;
        }

        public void Save(Order order)
        {
            if (order.OrderLine == null
                || !order.OrderLine.Any()
                || order.OrderLine.Any(line => line.Quantity <= 0))
            {
                throw new ArgumentException(
                    "An order must contain positive product quantities.",
                    nameof(order));
            }

            using var transaction = _context.Database.BeginTransaction(
                IsolationLevel.Serializable);

            var remainingStocks = new Dictionary<int, int>();

            // Vérifier tous les produits avant de modifier les données.
            foreach (var group in order.OrderLine
                .GroupBy(line => line.ProductId)
                .OrderBy(group => group.Key))
            {
                // La projection relit le stock en base sans réutiliser
                // la valeur d'une entité déjà suivie par EF.
                var inventory = _context.Product
                    .Where(product => product.Id == group.Key)
                    .Select(product => new
                    {
                        product.Id,
                        product.Quantity
                    })
                    .SingleOrDefault();

                long requestedQuantity = group.Sum(
                    line => (long)line.Quantity);

                if (inventory == null
                    || requestedQuantity > inventory.Quantity)
                {
                    throw new InsufficientStockException();
                }

                remainingStocks.Add(
                    inventory.Id,
                    inventory.Quantity - (int)requestedQuantity);
            }

            foreach (var stock in remainingStocks)
            {
                var product = _context.Product.Single(
                    product => product.Id == stock.Key);

                product.Quantity = stock.Value;
            }

            _context.Order.Add(order);

            // Enregistrer la commande et les nouveaux stocks ensemble.
            _context.SaveChanges();
            transaction.Commit();
        }

        public async Task<Order> GetOrder(int? id)
        {
            var orderEntity = await _context.Order.Include(x => x.OrderLine)
                .ThenInclude(product => product.Product).SingleOrDefaultAsync(m => m.Id == id);
            return orderEntity;
        }

        public async Task<IList<Order>> GetOrders()
        {
            var orders = await _context.Order.Include(x => x.OrderLine)
                .ThenInclude(product => product.Product).ToListAsync();
            return orders;
        }
    }
}