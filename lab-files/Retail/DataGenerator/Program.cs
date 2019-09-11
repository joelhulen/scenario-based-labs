﻿using Contoso.Apps.Common;
using Contoso.Apps.Movies.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] types =  new int[] { 1, 2, 3 };

            //fire off many threads of different personalities...
            DoWork(1);
            DoWork(2);
            DoWork(3);
        }

        static void DoWork(int personalityType)
        {
            //execute actions of a user...
            Guid sessionId = Guid.NewGuid();

            List<Item> movies = DbHelper.GetMoviesByType(personalityType); ;

            //loop...
            while (true)
            {
                //randomly get a movie
                Item p = GetRandomMovie(movies);

                //randomly do this x times / 
                DbHelper.GenerateAction(1, p.ItemId.ToString(), "details", sessionId.ToString().Replace("-", ""));
                DbHelper.GenerateAction(1, p.ItemId.ToString(), "buy", sessionId.ToString().Replace("-", ""));
                DbHelper.GenerateAction(1, p.ItemId.ToString(), "order", sessionId.ToString().Replace("-", ""));
            }
        }

        static Item GetRandomMovie(List<Item> movieSet)
        {
            Random r = new Random();
            return movieSet.Skip(r.Next(movieSet.Count)).Take(1).FirstOrDefault();
        }
    }
}