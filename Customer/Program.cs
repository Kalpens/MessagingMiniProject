using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using MessagingGateway;
using MiniProjectB2BRetailer.DTO;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MiniProjectB2BRetailer.Customer
{
    class Program
    {
        private static string customerID = null;

        static void Main(string[] args)
        {
            customerID = Guid.NewGuid().ToString("N");

            while (true)
            {
                Console.WriteLine("Welcom to B2B Retailer!\n" + "Please choose from the following menu points:");
                Console.WriteLine("1. Order\n" + "2. Exit");
                var answer = Console.ReadLine();
                switch (Convert.ToInt32(answer))
                {
                    case 1:
                        Console.WriteLine("Please fill out the following info on the order!\n" + "Item >> ");
                        var itemName = Console.ReadLine();
                        Console.WriteLine("Please fill out the following info on the order!\n" + "Country >> ");
                        var countryName = Console.ReadLine();
                        var order = new Order() { Item = new Item() { Name = itemName }, Location = new Location() { CountryName = countryName }, CustomerID = customerID };
                        var gateway = new SynchronousMessagingGateway();
                        //handle response here
                        var itemAtWarehouse = gateway.PurchaseOrder(order);
                        if (itemAtWarehouse.ItemAvailable == false)
                        {
                            Console.WriteLine("Item Not available");
                            Thread.Sleep(1000);
                            goto case 1;
                        }
                        else
                        {
                            Console.WriteLine("Your item shipping cost:" + itemAtWarehouse.Charge);
                            Console.WriteLine("Time to ship Your item: " + itemAtWarehouse.ShippingTime);
                            Console.WriteLine("Are you satisfied with your order? Y/N");
                            var ans = Console.ReadLine();
                            if (ans == "N")
                            {
                                Thread.Sleep(1000);
                                goto case 1;
                            }
                            else
                            {
                                Console.WriteLine("Thank you for your order.");
                                Thread.Sleep(1000);
                            }

                            break;
                        }
                    case 2:
                        Console.WriteLine("Thank you for using our application.");
                        Thread.Sleep(3000);
                        Environment.Exit(0);
                        break;

                }
            }
        }
        
    }
}
