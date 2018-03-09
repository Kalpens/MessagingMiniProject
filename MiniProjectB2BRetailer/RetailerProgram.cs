using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Consumer;
using MiniProjectB2BRetailer.DTO;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MiniProjectB2BRetailer
{
    class RetailerProgram
    {
        private static List<ItemResponse> itemResponseList = new List<ItemResponse>();
        private static Order receivedOrder = null;
        private static ItemResponse localItemResponse = null;
        static void Main(string[] args)
        {
            ListenToCustomerOrders();
            var continueOperation = true;
            while (continueOperation)
            {
                if (Console.ReadLine() == "Quit")
                {
                    break;
                }
            }
        }

        private static ItemResponse FindCheapestSolution()
        {
            Console.WriteLine("Searching for cheapest response");
            float shippingCharge = float.MaxValue;
            int shippingTime = int.MaxValue;
            int countInStock = 0;
            ItemResponse rightResponse = null;
            foreach (var itemResponse in itemResponseList)
            {
                if(itemResponse.Charge <= shippingCharge && itemResponse.ShippingTime <= shippingTime && itemResponse.ItemAvailable && countInStock < itemResponse.VolumeInStock)
                {
                    shippingCharge = itemResponse.Charge;
                    shippingTime = itemResponse.ShippingTime;
                    countInStock = itemResponse.VolumeInStock;
                    rightResponse = itemResponse;
                }
            }
            if (rightResponse == null)
                rightResponse = new ItemResponse(){CustomerId = itemResponseList[0].CustomerId};
            return rightResponse;
        }
        private static void ListenToCustomerOrders()
        {
            receivedOrder = null;
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                bus.Receive<Order>("RetailerExpectingOrders", order => HandleOrder(order));
                Console.WriteLine("Listening to customer Orders");
                Console.ReadLine();
            }
        }
        private static void HandleOrder(Order order)
        {
            if (order != null)
            {
                receivedOrder = order;
                Console.WriteLine("Received a order from Customer, order item:" + order.Item.Name);
                CreateOrderForLocalWarehouse(order);
            }
            else
            {
                Console.WriteLine("Received order was null, ignoring call.");
            }
            
        }

        private static void RespondToCustomer(ItemResponse itemResponse)
        {
            Console.WriteLine("Responsing to Customer of id" + itemResponse.CustomerId);
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                // Declare an exchange:
                var exchange = bus.Advanced.ExchangeDeclare("Customers", ExchangeType.Topic);
                var message = new Message<ItemResponse>(itemResponse);
                // Synchronous publisher:
                bus.Advanced.Publish(exchange, itemResponse.CustomerId, false, message);
            }
        }

        private static void ListenToWarehouseResponses()
        {
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                bus.Receive<ItemResponse>("RetailerForOrder", x => HandleItemResponse(x));
                Console.WriteLine("Listening for warehouse responses for 2 seconds");
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            var bestItemResponse = FindCheapestSolution();
            RespondToCustomer(bestItemResponse);
            itemResponseList = new List<ItemResponse>();
        }

        private static void HandleItemResponse(ItemResponse itemResponse)
        {
            itemResponseList.Add(itemResponse);
            Console.WriteLine("Received a response from warehouse \n item:" + itemResponse.Name + " \n Price: " + itemResponse.Price
                              + "\n Avaiable: " + itemResponse.ItemAvailable);
        }

        private static void CreateOrderForLocalWarehouse(Order order)
        {
            Console.WriteLine("Creating an order for local warehouse");
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                // Declare an exchange:
                var exchange = bus.Advanced.ExchangeDeclare("Warehouse.Direct", ExchangeType.Direct);
                var message = new Message<Order>(order);
                // Synchronous publisher:
                bus.Advanced.Publish(exchange, order.Location.CountryName, false, message);
            }
            ListenToLocalWarehouseResponse();
        }

        private static void ListenToLocalWarehouseResponse()
        {
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                bus.Receive<ItemResponse>("RetailerForLocalOrder", itemResponse => 
                    localItemResponse = itemResponse);
                Console.WriteLine("Listening for Warehouse response for 2 seconds");
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            if (localItemResponse.ItemAvailable)
            {
                Console.WriteLine("Received Response from Local warehouse, item is available");
                RespondToCustomer(localItemResponse);
            }
            else
            {
                Console.WriteLine("Received Response from Local warehouse, item is not available");
                CreateOrderForAllWarehouses(receivedOrder);
            }

            localItemResponse = null;
        }

        private static void CreateOrderForAllWarehouses(Order order)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "logs", type: "fanout");

                string json = JsonConvert.SerializeObject(order);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: "logs", 
                    routingKey: "", basicProperties: null, body: body);
                Console.WriteLine(" [+] Sent order to all warehouses");
            }

            ListenToWarehouseResponses();
        }
    }
}

