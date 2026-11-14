using System;

namespace P3AddNewFunctionalityDotNetCore.Models.Exceptions
{
    public class InsufficientStockException : Exception
    {
        public InsufficientStockException()
            : base("The requested quantity exceeds the available stock.")
        {
        }
    }
}