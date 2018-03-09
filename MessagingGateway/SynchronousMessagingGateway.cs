using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using EasyNetQ;
using MiniProjectB2BRetailer.DTO;
using RabbitMQ.Client;

namespace MessagingGateway
{
    public class SynchronousMessagingGateway
    {
        private ItemResponse ListenToRetailerResponse(string customerID)
        {
            ItemResponse response = null;
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                // Declare an exchange:
                var exchange = bus.Advanced.ExchangeDeclare("Customers", ExchangeType.Topic);
                // Declare a queue
                var queue = bus.Advanced.QueueDeclare("Customer" + customerID, false, true, false, true);
                // Bind queue to exchange (the routing key is ignored by a fanout exchange):
                bus.Advanced.Bind(exchange, queue, customerID);
                // Synchronous consumer:
                bus.Advanced.Consume<ItemResponse>(queue, (message, info) =>
                    response = message.Body );
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            return response;
        }

        public ItemResponse PurchaseOrder(Order order)
        {
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                bus.Send("RetailerExpectingOrders", order);
            }

            ItemResponse response = null;
            int counter = 0;
            while (response == null)
            {
                response = ListenToRetailerResponse(order.CustomerID);
                counter++;
                if (counter == 20)
                    throw new Exception("No response in 20 seconds");
            }

            return response;
        }
    }
}
