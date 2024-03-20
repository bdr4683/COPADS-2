/// <summary>
/// NumGen takes in 3 command line arguments and generates a series of prime numbers
/// or random odd numbers and their number of factors
/// <\summary>

using System;
using System.Numerics;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Data.SqlTypes;
using System.Collections;
using System.Reflection.Metadata;
using System.Dynamic;
using System.Diagnostics;

class Program {

    static int primeBits = 0;
    static int primeMaxCount = 0;
    static int primeCount = 0;
    static BigInteger[] primeNumbers = new BigInteger[0];

    static Stopwatch watch = new Stopwatch();

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

        watch.Start();

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
        watch.Stop();
        Console.WriteLine("BitLength: " + bits + " bits");
        int i = 1;
        foreach(OddResults number in results) {
            Console.WriteLine(i + ": " + number.bigInt);
            Console.WriteLine("Number of factors: " + number.factors);
            i++;
        }
        Console.WriteLine("Time to Generate: " + watch.Elapsed);
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
        primeBits = bits;
        primeMaxCount = count;
        primeNumbers = new BigInteger[primeMaxCount];
        for(int x = 0; x < Environment.ProcessorCount * 2; x++) {
            Thread thread = new Thread(new ThreadStart(GeneratePrimes));
            thread.Start();
        }

        foreach(BigInteger num in primeNumbers) {
            if(num.IsZero) {
                primeCount = 0;
                Prime(bits, count);
                return;
            }
        }

        watch.Stop();
        Console.WriteLine("BitLength: " + bits + " bits");
        int i = 1;
        foreach(BigInteger num in primeNumbers) {
            Console.WriteLine(i + ": " + num);
            i++;
        }
        Console.WriteLine("Time to Generate: " + watch.Elapsed);
    }

    /// <summary>
    /// A Stack Overflow Aided version of the Miller-Rabin primality test
    /// </summary>
    /// <param name="bigInt">Candidate number</param>
    /// <returns>True if number is prime (to a reasonable degree of certainty), 
    /// false otherwise</returns>
    private static bool isPrime(BigInteger bigInt) {
        if(bigInt == 2 || bigInt == 3)
            return true;
        if(bigInt < 2 || bigInt % 2 == 0)
            return false;

        BigInteger d = bigInt - 1;
        int s = 0;

        while(d % 2 == 0)
        {
            d /= 2;
            s += 1;
        }

        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[bigInt.ToByteArray().LongLength];
        
        BigInteger a;

        for(int i = 0; i < 10; i++)
        {
            do {
                rng.GetBytes(bytes);
                a = new BigInteger(bytes);
            } while(a < 2 || a >= bigInt - 2);

            BigInteger x = BigInteger.ModPow(a, d, bigInt);
            if(x == 1 || x == bigInt - 1)
                continue;

            for(int r = 1; r < s; r++)
            {
                x = BigInteger.ModPow(x, 2, bigInt);
                if(x == 1)
                    return false;
                if(x == bigInt - 1)
                    break;
            }

            if(x != bigInt - 1)
                return false;
        }

        return true;
    }


    private static void GeneratePrimes() {
        while(primeCount < primeMaxCount) {
            BigInteger candidate = GenerateNum(primeBits);
            if(isPrime(candidate)) {
                int index = primeCount;
                if(index < primeMaxCount) {
                    primeNumbers[index] = candidate;
                }
                primeCount++;
            }
        }
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

/// <summary>
/// A helper class to better contain the results of generating prime numbers
/// and their number of factors. Allows results to be stored in a single 
/// collection without making it multidimensional
/// </summary>
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