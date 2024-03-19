/// <summary>
/// [finish comment here]
/// <\summary>

using System;
using System.Numerics;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

class Program {

    static int bits = 0;

    static int 
    static int count = 0;

    public static void Main(string[] args) {

        if(args.Length != 3) {
            PrintUsage();
            return;
        }

        if(args[0]%8 == 0) {
            bits = args[0];
        }
        else {
            PrintUsage();
            return;
        }

        count = args[2];

        switch(args[1]) {
            

        }


        
    }

    private static void PrintUsage() {
        Console.WriteLine("Usage: NumGen <bits> <option> <count>\n" +
                          "bits: bits of num to generate (must be multiple of 8)" +
                          "option: 'odd' or 'prime" +
                          "count: count of numbers to generate");
    }

}