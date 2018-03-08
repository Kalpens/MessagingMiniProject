using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using MiniProjectB2BRetailer.DTO;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MiniProjectB2BRetailer.WarehouseApp
{
    class WarehouseProgram
    {
        private static Warehouse stock;
        private static IBus bus;
        static void Main(string[] args)
        {
            SetupVariables(args[0], int.Parse(args[1]));

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "logs", type: "fanout");

                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: queueName, exchange: "logs", routingKey: "");

                Console.WriteLine("Waiting for orders.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Order receivedOrder = JsonConvert.DeserializeObject<Order>(message);
                    Console.WriteLine(" [x] Received order, checking database if contains item...");
                    HandleOrderRequest(receivedOrder, "");
                };
                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                ListenForLocalOrders();
            }

            Console.WriteLine("Write Quit to stop");
            var continueOperation = true;
            while (continueOperation)
            {
                if (Console.ReadLine() == "Quit")
                {
                    break;
                }
            }
        }
        private static void SetupVariables(string country, int quantity)
        {
            Console.WriteLine(country + " is running!");
            var itemList = new List<Item>();
            itemList.Add(new Item(){ID = 1, Name = "Phone", Price = 420, Quantity = quantity});
            itemList.Add(new Item() { ID = 2, Name = "BlackBoard", Price = 550, Quantity = 100 });
            itemList.Add(new Item() { ID = 3, Name = "Watch", Price = 60, Quantity = 200 });
            stock = new Warehouse
            {
                Location = new Location
                {
                    CountryName = country
                },
                Items = itemList
            };
        }

        private static void ListenForLocalOrders()
        {
            using (bus = RabbitHutch.CreateBus("host=localhost"))
            {
                // Declare an exchange:
                var exchange = bus.Advanced.ExchangeDeclare("Warehouse.Direct", ExchangeType.Direct);

                // Declare a queue
                var queue = bus.Advanced.QueueDeclare("Warehouse"+stock.Location.CountryName);

                // Bind queue to exchange (the routing key is ignored by a fanout exchange):
                bus.Advanced.Bind(exchange, queue, stock.Location.CountryName);
                // Synchronous consumer:
                bus.Advanced.Consume<Order>(queue, (message, info) =>
                    HandleOrderRequest(message.Body, "Local")
                );
                Console.WriteLine("Listening for local orders.");
                Console.ReadLine();
            }
        }

        private static void HandleOrderRequest(Order order, string orderType)
        {
            Console.WriteLine("Received an order Request");
            var shippingCost = 5F;
            var shippingTime = 2;
            if (order.Location.CountryName != stock.Location.CountryName)
            {
                shippingCost = 10F;
                shippingTime = 10;
            }
            bool foundMatchingItem = false;
            foreach (var item in stock.Items)
            {
                if (item.Name == order.Item.Name)
                {
                    foundMatchingItem = true;
                    if (item.Quantity < 1)
                    {
                        var responseItem = new ItemResponse() { Name = order.Item.Name, Charge = 0, ShippingTime = 0, ItemAvailable = false, Price = item.Price, WarehouseId = stock.Location.CountryName, CustomerId = order.CustomerID, VolumeInStock = item.Quantity };
                        SendResponseToRetailer(responseItem, orderType);
                    }
                    else
                    {
                        var responseItem = new ItemResponse() { Name = order.Item.Name, Charge = shippingCost, ShippingTime = shippingTime, ItemAvailable = true, Price = item.Price, WarehouseId = stock.Location.CountryName, CustomerId = order.CustomerID, VolumeInStock = item.Quantity};
                        SendResponseToRetailer(responseItem, orderType);
                    }
                }
            }
            if (foundMatchingItem == false)
            {
                Console.WriteLine("Item was not found in stock.");
                var responseItem = new ItemResponse() { Name = order.Item.Name, Charge = 0, ShippingTime = 0, ItemAvailable = false, Price = 0, WarehouseId = stock.Location.CountryName, CustomerId = order.CustomerID, VolumeInStock = 0};
                SendResponseToRetailer(responseItem, orderType);
            }
        }
        private static void SendResponseToRetailer(ItemResponse itemResponse, string orderType)
        {
            Console.WriteLine("Sending a response to retailer of available item");
            bus.Send("RetailerFor"+ orderType + "Order", itemResponse);
            //bus.Publish<ItemResponse>(itemResponse);
        }
    }
}
