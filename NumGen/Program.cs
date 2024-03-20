/// <summary>
/// [finish comment here]
/// <\summary>

using System;
using System.Numerics;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Data.SqlTypes;
using System.Collections;
using System.Reflection.Metadata;

class Program {

    public static void Main(string[] args) {

        int bits = 0;
        int count = 0;

        if(args.Length != 3) {
            PrintUsage();
            return;
        }

        if(int.TryParse(args[0], out bits)) {
            if(bits < 32 || bits % 8 != 0) {
                PrintUsage();
                return;
            }
        }
        else {
            PrintUsage();
            return;
        }

        if(!int.TryParse(args[2], out count)) {
            PrintUsage();
            return;
        }

        switch(args[1]) {
            case "odd":
                Odd(bits, count);
                break;
            case "prime":
                Prime(bits, count);
                break;
            default:
                PrintUsage();
                return;
        }


    }

    private static void Odd(int bits, int count) {
        List<OddResults> results = new List<OddResults>();
        Parallel.For(0, count, i =>
            {
                BigInteger bigInt;
                do {
                bigInt = GenerateNum(bits);
                }while (bigInt.IsEven);
        
                if(bigInt.Sign < 0) {
                    bigInt = -bigInt;
                }
                
                results.Add(new OddResults(bigInt, findFactors(bigInt)));
            });

        //TODO: print
        Console.WriteLine("BitLength: " + bits + " bits");
        int i = 1;
        foreach(OddResults number in results) {
            Console.WriteLine(i + ": " + number.bigInt);
            Console.WriteLine("Number of factors: " + number.factors);
            i++;
        }

    }

    private static int findFactors(BigInteger bigInt) {
        int numFactors = 0;
        BigInteger result = 1;
        BigInteger factor = 0;
        Console.WriteLine(bigInt);
        while(factor.CompareTo(result) < 0) {
            factor += 1;
            BigInteger remainder = bigInt % factor;
            if(remainder.IsZero) {
                numFactors += 2;
            }
            result = bigInt/factor;
        }

        return numFactors;
    }

    private static void Prime(int bits, int count) {

    }

    /// <summary>
    /// Generates a random BigInteger
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    private static BigInteger GenerateNum(int bits) {
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[bits/8];
        rng.GetBytes(bytes);
        return new BigInteger(bytes);
    }

    private static void PrintUsage() {
        Console.WriteLine("Usage: NumGen <bits> <option> <count>\n" +
                          "bits: bits of num to generate (must be multiple of 8)" +
                          "option: 'odd' or 'prime" +
                          "count: count of numbers to generate");
    }

}

class OddResults {
    public BigInteger bigInt { get; set; }
    public int factors { get; set; }

    public OddResults() {
        bigInt = 0;
        factors = 0;
    }

    public OddResults(BigInteger bigInt, int factors) {
        this.bigInt = bigInt;
        this.factors = factors;
    }

}