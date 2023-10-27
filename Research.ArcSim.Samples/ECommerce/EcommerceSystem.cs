using Research.ArcSim.Extensions;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Logical;
using AS = Research.ArcSim.Modeling;

namespace Research.ArcSim.Samples.ECommerce
{
    public class EcommerceSystem : AS.System
    {
        public EcommerceSystem()
        {
            AddInventoryModule();
            AddOrderingModule();
        }

        private void AddInventoryModule()
        {
            var inventoryModule = Modules.AddX(new Module
            {
                Name = "Inventory",
                Description = "Managed invnetory and provide information on how many of each items exists",
            });

            var searchFunction = inventoryModule.Functions.AddX(new Function
            {
                Name = "Search",
                Description = "Searches for an item and returned its quantity",
            });

            searchFunction.Activities.Add(new ActivityDefinition
            {
                Name = "Search UI",
                Description = "Allows user to search for an item",
                Layer = Layer.UI
            });

            searchFunction.Activities.Add(new ActivityDefinition
            {
                Name = "Lookup Item",
                Description = "Tries to find the item either locally or by calling an API",
                Layer = Layer.UI
            });

            searchFunction.Activities.Add(new ActivityDefinition
            {
                Name = "Lookup API",
                Description = "The API to look up and item",
                Layer = Layer.API,
            });

            searchFunction.Activities.Add(new ActivityDefinition
            {
                Name = "Item Select",
                Description = "The Select statement on DB to find the item",
                Layer = Layer.DB,
            });

            var sellFunction = inventoryModule.Functions.AddX(new Function
            {
                Name = "Selling an item",
                Description = "Updates the availalbe quantity of an item and triggers a refill if necessary",
            });

            searchFunction.Activities.Add(new ActivityDefinition
            {
                Name = "Sell API",
                Description = "Reduces the available quantity for an item and triggers a refill if necessary",
                Layer = Layer.API,
            });

            searchFunction.Activities.Add(new ActivityDefinition
            {
                Name = "Sell Update",
                Description = "Updates the number of avaialble items",
                Layer = Layer.DB,
            });
        }

        private void AddOrderingModule()
        {
            var orderingModule = Modules.AddX(new Module
            {
                Name = "Ordering",
                Description = "Handles order placemanet and fulfilment.",
            });

            var addItemFunction = orderingModule.Functions.AddX(new Function
            {
                Name = "Add Item",
                Description = "Add an item, seleting item attributes, and quantities",
            });

            addItemFunction.Activities.AddX(new ActivityDefinition
            {
                Name = "Add Item UI",
                Description = "The UI to select and item, attributes, and quantities to buy",
                Layer = Layer.UI
            });

            addItemFunction.Activities.AddX(new ActivityDefinition
            {
                Name = "Calculate Price API",
                Description = "The API to calculate the order price",
                Layer = Layer.API
            });

            addItemFunction.Activities.AddX(new ActivityDefinition
            {
                Name = "Confirm Order",
                Description = "The UI to confirm the order",
                Layer = Layer.UI
            });

            var payFunction = orderingModule.Functions.AddX(new Function
            {
                Name = "Pay",
                Description = "Handles making a payment",
            });

            payFunction.Activities.AddX(new ActivityDefinition
            {
                Name = "Payment UI",
                Description = "The UI used to make a payment",
                Layer = Layer.UI
            });

            payFunction.Activities.AddX(new ActivityDefinition
            {
                Name = "Payment API",
                Description = "The API to handle the payment",
                Layer = Layer.API
            });

            payFunction.Activities.AddX(new ActivityDefinition
            {
                Name = "Finalize Order API",
                Description = "The API to finalize order",
                Layer = Layer.API
            });

            payFunction.Activities.AddX(new ActivityDefinition
            {
                Name = "Create Order",
                Description = "The create store procedure",
                Layer = Layer.DB
            });
        }
    }
}

