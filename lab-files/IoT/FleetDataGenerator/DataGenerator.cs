﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using CosmosDbIoTScenario.Common;
using CosmosDbIoTScenario.Common.Models;

namespace FleetDataGenerator
{
    public static class DataGenerator
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates 1,000 vehicles.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Vehicle> GenerateVehicles()
        {
            var vehicles = new List<Vehicle>();
            const double defaultBatteryCycles = 200.0;
            var vins = VehicleTelemetryGenerator.GetVinMasterList();

            foreach (var vin in vins)
            {
                var batteryAgeDays = RandomIntegerInRange(1600, 20);
                var averageDailyBatteryCycles = RandomDoubleInRange(0.1852, 0.1056);
                vehicles.Add(new Vehicle
                {
                    id = Guid.NewGuid().ToString(),
                    vin = vin,
                    batteryRatedCycles = defaultBatteryCycles,
                    batteryAgeDays = batteryAgeDays,
                    lifetimeBatteryCyclesUsed = batteryAgeDays * averageDailyBatteryCycles,
                    lastServiceDate = RandomDateBeforeToday(400),
                    stateVehicleRegistered = VehicleTelemetryGenerator.GetLocation()
                });
            }

            return vehicles;
        }

        public static IEnumerable<Consignment> GenerateConsignments(int numberToGenerate = 1000)
        {
            var consignments = new List<Consignment>();

            for (var i = 0; i < numberToGenerate; i++)
            {
                consignments.Add(new Consignment
                {
                    id = Guid.NewGuid().ToString(),
                    customer = RandomCompanyName(),
                    status = WellKnown.Status.Pending,
                    deliveryDueDate = DateTime.UtcNow.AddMinutes(RandomIntegerInRange(510, -10))
                });
            }

            return consignments;
        }

        public static IEnumerable<Package> GeneratePackages(List<Consignment> consignments)
        {
            var packages = new List<Package>();
            var averageStorageTemperature = 30.0;
            
            foreach (var consignment in consignments)
            {
                var numberOfPackagesToAdd = RandomIntegerInRange(350, 25);
                consignment.attachedPackages = new List<Package>();

                for (var i = 0; i < numberOfPackagesToAdd; i++)
                {
                    var newPackage = new Package
                    {
                        id = Guid.NewGuid().ToString(),
                        consignmentId = consignment.id,
                        length = Math.Round(RandomDoubleInRange(56, 4), 2),
                        height = Math.Round(RandomDoubleInRange(56, 4), 2),
                        width = Math.Round(RandomDoubleInRange(56, 4), 2),
                        grossWeight = Math.Round(RandomDoubleInRange(250, 1.5), 2),
                        storageTemperature = Math.Round(RandomizeInitialValue(averageStorageTemperature, 0.3), 0),
                        highValue = _random.Next(100) % 25 == 0,
                        consignment = new PackageConsignment
                        {
                            consignmentId = consignment.id,
                            customer = consignment.customer,
                            deliveryDueDate = consignment.deliveryDueDate
                        }
                    };

                    packages.Add(newPackage);
                    consignment.attachedPackages.Add(newPackage);
                }
            }

            return packages;
        }

        public static IEnumerable<Trip> GenerateTrips(List<Consignment> consignments, List<Vehicle> vehicles)
        {
            var trips = new List<Trip>();
            var assignedVINs = new List<string>();

            foreach (var consignment in consignments)
            {
                var consignmentPackages = new List<List<Package>>();
                var numPackages = consignment.attachedPackages.Count;
                // If there are more than 300 packages, divide them between two trips.
                if (numPackages > 300)
                {
                    // Divide into two trips. Could divide in other ways by changing the size parameter of the Partition extension method.
                    consignmentPackages = consignment.attachedPackages.Partition(numPackages / 2).ToList()
                        .Select(x => x.ToList())
                        .ToList();

                    // Sanity check:
                    if (consignmentPackages.Count > 2)
                    {
                        // Add third list to second and remove third list.
                        consignmentPackages[1].AddRange(consignmentPackages[2]);
                        consignmentPackages.Remove(consignmentPackages[2]);
                    }
                }
                else
                {
                    consignmentPackages.Add(consignment.attachedPackages);
                }

                // Loop through packages to create new trips.
                foreach (var packageList in consignmentPackages)
                {
                    var vehicle = vehicles.FirstOrDefault(x => !assignedVINs.Contains(x.vin));

                    // Make sure there's a vehicle available to transport the packages.
                    if (vehicle != null)
                    {
                        var packages = new List<TripPackage>();

                        var newTrip = new Trip
                        {
                            id = Guid.NewGuid().ToString(),
                            consignmentId = consignment.id,
                            vin = vehicle.vin,
                            status = WellKnown.Status.Pending,
                            plannedTripDistance = RandomDoubleInRange(250, 30)
                        };

                        foreach (var package in packageList)
                        {
                            package.tripId = newTrip.id;
                            package.trip = new PackageTrip
                            {
                                tripId = newTrip.id,
                                plannedTripDistance = newTrip.plannedTripDistance,
                                vin = newTrip.vin
                            };

                            packages.Add(new TripPackage
                            {
                                grossWeight = package.grossWeight,
                                height = package.height,
                                highValue = package.highValue,
                                length = package.length,
                                packageId = package.id,
                                storageTemperature = package.storageTemperature,
                                width = package.width
                            });
                        }

                        newTrip.packages = packages;

                        assignedVINs.Add(vehicle.vin);
                        trips.Add(newTrip);
                    }
                }

            }

            return trips;
        }

        public static double RandomDoubleInRange(double max, double min)
        {
            return _random.NextDouble() * (max - min) + min;
        }

        public static int RandomIntegerInRange(int max, int min)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Creates a random value in a range of += deviateUpDownPercentage (%) of the initial value.
        /// </summary>
        /// <param name="initialValue">The initial value you wish to randomize.</param>
        /// <returns></returns>
        private static double RandomizeInitialValue(double initialValue, double deviateUpDownPercentage = 0.02)
        {
            var upper = initialValue + (initialValue * deviateUpDownPercentage);
            var lower = initialValue - (initialValue * deviateUpDownPercentage);

            return _random.NextDouble() * (upper - lower) + lower;
        }

        public static DateTime RandomDateBeforeToday(int maxDaysAgo)
        {
            var randomDaysAgo = RandomIntegerInRange(maxDaysAgo, 1);
            // Subtract random days from current date and return.
            return DateTime.UtcNow.AddDays(-randomDaysAgo);
        }

        public static string RandomCompanyName()
        {
            var list = new List<string>() { "Adatum Corporation", "Adventure Works Cycles", "Alpine Ski House", "Bellows College",
                                            "Best For You Organics Company", "Contoso Pharmaceuticals", "Contoso Suites",
                                            "Consolidated Messenger", "Fabrikam, Inc.", "Fabrikam Residences", "Fincher Architects",
                                            "First Up Consultants", "Fourth Coffee", "Graphic Design Institute", "Humongous Insurance",
                                            "Lamna Healthcare Company", "Liberty's Delightful Sinful Bakery & Cafe", "Lucerne Publishing",
                                            "Margie's Travel", "Munson's Pickles and Preserves Farm", "Nod Publishers", "Northwind Traders",
                                            "Proseware, Inc.", "Relecloud", "School of Fine Art", "Southridge Video", "Tailspin Toys",
                                            "Trey Research", "The Phone Company", "VanArsdel, Ltd.", "Wide World Importers", "Wingtip Toys",
                                            "Woodgrove Bank" };
            var l = list.Count;
            var num = _random.Next(l);
            return list[num];
        }
    }
}