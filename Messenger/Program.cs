///
/// Program.cs contains functionality for generating, sending, and recieving RSA
///  encryption keys as well as encrypting and decrypting messages that can be
///  sent to and recieved from an external server
///
/// Author: Brandon Ranallo
/// Date: 4/12/2024
///

using System;
using System.Numerics;
using System.IO;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using System.Data.SqlTypes;
using System.Collections;
using System.Reflection.Metadata;
using System.Dynamic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Buffers.Binary;


class Program {

    /// <summary>
    /// Main takes in all command line arguments and performs the appropriate task, printing a usage message when arguments are bad
    /// </summary>
    /// <param name="args">Command line arguments</param>
    static void Main(string[] args) {
        try{
            switch(args[0]) {
                case "keyGen":
                    int keysize = 0;
                    if(args.Length > 1 && int.TryParse(args[1], out keysize)) {
                        KeyGen(keysize).Wait();
                    }
                    else {
                        PrintUsage();
                    }
                    break;
                case "sendKey":
                    if(args.Length > 1) {
                        SendKey(args[1]).Wait();
                    }
                    else {
                        PrintUsage();
                    }
                    break;
                case "getKey":
                    if(args.Length > 1) {
                        GetKey(args[1]).Wait();
                    }
                    else {
                        PrintUsage();
                    }
                    break;
                case "sendMsg":
                    if(args.Length > 2) {
                        string message = string.Join(" ", args, 2, args.Length - 2);
                        SendMsg(args[1], message).Wait();
                    }
                    else {
                        PrintUsage();
                    }
                    break;
                case "getMsg":
                    if(args.Length > 1) {
                        GetMsg(args[1]).Wait();
                    }
                    else {
                        PrintUsage();
                    }
                    break;
                default:
                    PrintUsage();
                    break;
            }
        }
        catch {
            Console.WriteLine("Oh... Something unexpected went wrong...");
        }
    }

    /// <summary>
    /// Generates public and private keys to be stored locally in public.key and private.key
    /// </summary>
    /// <param name="keysize">keysize - in bits - to be used for encryption</param>
    /// <returns>n/a</returns>
    static async Task KeyGen(int keysize) {
        Random random = new Random();
        int mult = 1;
        if(random.Next(0, 2) == 1)
            mult = -1;
        int pSize = keysize/2 + (random.Next(20, 30) * mult);
        int qSize = keysize - pSize;
        int ESize = 16;

        BigInteger p = PrimeNumbers.Prime(pSize);
        BigInteger q = PrimeNumbers.Prime(qSize);
        BigInteger NBig = BigInteger.Multiply(p, q);
        BigInteger T = BigInteger.Multiply(p-1, q-1);
        BigInteger EBig = PrimeNumbers.Prime(ESize);
        BigInteger DBig = modInverse(EBig, T);

        //Convert all numbers to properly formatted byte arrays (reversing endianness on length numbers to make them big endian)
        byte[] N = NBig.ToByteArray();
        byte[] n = BitConverter.GetBytes(N.Length);
        byte[] E = EBig.ToByteArray();
        byte[] e = BitConverter.GetBytes(E.Length);
        byte[] D = DBig.ToByteArray();
        byte[] d = BitConverter.GetBytes(D.Length);

        //Create public key byte array by block copying from other byte arrays in order
        byte[] publicBytes = new byte[e.Length + E.Length + n.Length + N.Length];
        System.Buffer.BlockCopy(e, 0, publicBytes, 0, e.Length);
        System.Buffer.BlockCopy(E, 0, publicBytes, e.Length, E.Length);
        System.Buffer.BlockCopy(n, 0, publicBytes, e.Length + E.Length, n.Length);
        System.Buffer.BlockCopy(N, 0, publicBytes, e.Length + E.Length + n.Length, N.Length);

        //Create private key byte array by block copying from other byte arrays in order
        byte[] privateBytes = new byte[d.Length + D.Length + n.Length + N.Length];
        System.Buffer.BlockCopy(d, 0, privateBytes, 0, d.Length);
        System.Buffer.BlockCopy(D, 0, privateBytes, d.Length, D.Length);
        System.Buffer.BlockCopy(n, 0, privateBytes, d.Length + D.Length, n.Length);
        System.Buffer.BlockCopy(N, 0, privateBytes, d.Length + D.Length + n.Length, N.Length);

        PublicKey publicKey = new PublicKey(Convert.ToBase64String(publicBytes), "");
        publicKey.WriteToFile();

        PrivateKey privateKey = new PrivateKey(Convert.ToBase64String(privateBytes));
        privateKey.WriteToFile();

    }

    /// <summary>
    /// Sends the key from public.key to the specified user on the server and adds that user's email to the list of emails in private.key
    /// </summary>
    /// <param name="email">email of user to send the key to</param>
    /// <returns></returns>
    static async Task SendKey(string email) {
        using (HttpClient client = new HttpClient()) {
            PublicKey key;
            try {
                key = PublicKey.RetrieveKeyInfo();
            }
            catch(FileNotFoundException ex) {
                Console.WriteLine("No local public key found. Run keyGen <keysize> to generate local keys.");
                return;
            }
            key.email = email;

            try {

                HttpResponseMessage response = await client.PutAsync("http://voyager.cs.rit.edu:5050/Key/" + email, new StringContent(JsonSerializer.Serialize(key), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode) {
                    PrivateKey.AddEmail(email);
                    Console.WriteLine("Key saved");
                }
                else {
                    Console.WriteLine("Http Put Error");
                }

            }
            catch(HttpRequestException e) {
                Console.WriteLine("Error: " + e.Message);
            }

        }
    }    

    /// <summary>
    /// Retrieves a key from a user on the server and stores it in a file named <paramref name="email"/>.key
    /// </summary>
    /// <param name="email">email of user to get key from</param>
    /// <returns></returns>
    static async Task GetKey(string email) {
        using (HttpClient client = new HttpClient()) {

            try {
                
                HttpResponseMessage response = await client.GetAsync("http://voyager.cs.rit.edu:5050/Key/" + email);

                if (response.IsSuccessStatusCode) {
                    
                    string responseBody = await response.Content.ReadAsStringAsync();
                    if(responseBody == "") {
                        Console.WriteLine("User has not been created or has no public key. Provide them with a key by running sendKey <user email>.");
                        return;
                    }

                    JsonDocument body = JsonDocument.Parse(responseBody);
                    PublicKey key = JsonSerializer.Deserialize<PublicKey>(body);

                    key.WriteToFile(email);   
                    
                }
                else{
                    Console.WriteLine("Failed");
                }

            }
            catch (HttpRequestException e) {
                //TODO
                Console.WriteLine("Error: " + e.Message);
                return;
            }
        }
    }

    /// <summary>
    /// Sends an encoded and encrypted message to the specified user.
    /// </summary>
    /// <param name="email">email of remote user to send the message to</param>
    /// <param name="content">plaintext content of the message to send</param>
    /// <returns></returns>
    static async Task SendMsg(string email, string content) {
        using (HttpClient client = new HttpClient()) {
            try {
                PublicKey userKey = PublicKey.GetUserKey(email);
                string encryptedMsg = Encrypt(userKey.key, content);
                string message = (new Message(email, encryptedMsg)).ToString();
                HttpResponseMessage response = await client.PutAsync("http://voyager.cs.rit.edu:5050/Message/" + email, new StringContent(message, Encoding.UTF8, "application/json"));
                if(response.IsSuccessStatusCode) {
                    Console.WriteLine("Message Written");
                    return;
                }
            }
            catch (FileNotFoundException ex) {
                Console.WriteLine("Public key not stored locally for this user. Run getKey <user> to retrieve user's encryption key.");
                return;
            }
            catch (HttpRequestException e) {
                Console.WriteLine("Error uploading message to user: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Retrieves the message stored by the specified user on the server and prints the message on the command line
    /// </summary>
    /// <param name="email">email of user to retrieve message from</param>
    /// <returns></returns>
    static async Task GetMsg(string email) {
        using(HttpClient client = new HttpClient()) {
            try {
                
                HttpResponseMessage response = await client.GetAsync("http://voyager.cs.rit.edu:5050/Message/" + email);

                if (response.IsSuccessStatusCode) {
                    
                    string responseBody = await response.Content.ReadAsStringAsync();
                    if(responseBody == "") {
                        Console.WriteLine("User has not been created or has no message. Provide them with a message by running sendMsg <user email> <message>");
                        return;
                    }

                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
                    JsonElement root = jsonDocument.RootElement;

                    string content = root.GetProperty("content").GetString();

                    Message message = new Message(email, content);
                    
                    PrivateKey privateKey = PrivateKey.RetrieveKeyInfo();

                    if(!privateKey.emails.Contains(message.email)) {
                        Console.WriteLine("No private key saved for this user. Run sendKey <user> to obtain key.");
                        return;
                    }

                    string result = Decrypt(privateKey.key, message.content);
                    Console.WriteLine(result);              
                    
                }
                else{
                    Console.WriteLine("Failed");
                }

            }
            catch (HttpRequestException e) {
                //TODO
                Console.WriteLine("Error: " + e.Message);
                return;
            }
            

            
        }
    }

    /// <summary>
    /// Performs the modpow encryption algorithm on a message given a proper key
    /// </summary>
    /// <param name="key">encryption key as a base 64 encoded string</param>
    /// <param name="content">plaintext message to be encrypted</param>
    /// <returns></returns>
    static string Encrypt(string key, string content) {
        byte[] keyBytes = Convert.FromBase64String(key);

        int e = BitConverter.ToInt32(keyBytes, 0);

        byte[] EArr = new byte[e];
        Array.Copy(keyBytes, 4, EArr, 0, e);
        BigInteger E = new BigInteger(EArr);

        int n = BitConverter.ToInt32(keyBytes, 4 + e);
        byte[] NArr = new byte[n];
        Array.Copy(keyBytes, 4 + e + 4, NArr, 0, n);
        BigInteger N = new BigInteger(NArr);

        byte[] msgBytes = Encoding.UTF8.GetBytes(content);
        BigInteger msg = new BigInteger(msgBytes);

        msg = BigInteger.ModPow(msg, E, N);

        msgBytes = msg.ToByteArray();
        return Convert.ToBase64String(msgBytes);
    }

    /// <summary>
    /// Decrypts an encrypted message given the decryption key
    /// </summary>
    /// <param name="key">decryption key as a base64 encoded string</param>
    /// <param name="content">base64 encoded message to decrypt</param>
    /// <returns></returns>
    static string Decrypt(string key, string content) {
        byte[] keyBytes = Convert.FromBase64String(key);

        int d = BitConverter.ToInt32(keyBytes, 0);

        byte[] DArr = new byte[d];
        Array.Copy(keyBytes, 4, DArr, 0, d);
        BigInteger D = new BigInteger(DArr);

        int n = (BitConverter.ToInt32(keyBytes, 4 + d));
        byte[] NArr = new byte[n];
        Array.Copy(keyBytes, 4 + d + 4, NArr, 0, n);
        BigInteger N = new BigInteger(NArr);

        

        byte[] msgBytes = Convert.FromBase64String(content);
        BigInteger msg = new BigInteger(msgBytes);

        msg = BigInteger.ModPow(msg, D, N);

        msgBytes = msg.ToByteArray();
        return Encoding.UTF8.GetString(msgBytes);
    }

    /// <summary>
    /// Provided modInverse function
    /// </summary>
    /// <param name="a">E</param>
    /// <param name="b">T</param>
    /// <returns>D</returns>
    static BigInteger modInverse(BigInteger a, BigInteger b)
    {
        BigInteger i = b, v = 0, d = 1;
        while (a>0) {
            BigInteger z = i/a, x = a;
            a = i % x;
            i = x;
            x = d;
            d = v - z*x;
            v = x;
        }
        v %= b;
        if (v<0) v = (v+b) % b;
        return v;
    }

    /// <summary>
    /// Prints proper command line usage of program
    /// </summary>
    private static void PrintUsage() {
        Console.WriteLine("Usage: dotnet run <option> <option arguments>\n" +
                          "Options:\n" +
                          "\tkeyGen <keysize>: Generates new public and private keys to be stored locally.\n" +
                          "\tsendKey <email>: Sends the local public key to the specified user and stores it on the server.\n" +
                          "\tgetKey <email>: Retrieves the public key from the specified user and stores it locally for future use.\n" +
                          "\tsendMsg <email> <plaintext>: Sends an encrypted version of the plaintext message to the specified user given there is a key stored for that user.\n" +
                          "\tgetMsg <email>: Retrieves the encrypted message of the given user and decrypts it if the private key for that user is locally stored.");
    }

}

/// <summary>
/// Json serializable class for messages
/// </summary>
class Message {
    public string email { get; set; }
    public string content { get; set; }

    public Message(string email, string content) {
        this.email = email;
        this.content = content;
    }

    public string ToString() {
        return JsonSerializer.Serialize(this);
    }

}

/// <summary>
/// Json serializable class for private key
/// </summary>
class PrivateKey {
    //string representation of the list of emails (for serialization purposes)
    public string email { get; set; }
    //string representing the key
    public string key { get; set; }
    //list of emails to be used in actual operations
    public List<string> emails;

    //Creates a PrivateKey instance by reading the JSON string from the private.key file and returns it
    public static PrivateKey RetrieveKeyInfo() {
        string keyInfo = File.ReadAllText("private.key");
        PrivateKey result = JsonSerializer.Deserialize<PrivateKey>(keyInfo);
        result.emails = new List<string>(result.email.Split(", ", StringSplitOptions.RemoveEmptyEntries));
        return result;
    }

    //Constructor for a Private Key
    public PrivateKey(string key) {
        this.key = key;
        emails = new List<string>();
        email = string.Join(", ", emails);
    }

    //Serializes the PrivateKey object and writes it to the private.key file
    public void WriteToFile() {
        email = string.Join(", ", emails);
        File.WriteAllText("private.key", JsonSerializer.Serialize(this));
    }

    //Public facing method to add an email to the email list and write the updated object to private.key
    public static void AddEmail(string user) {
        RetrieveKeyInfo().Add(user).WriteToFile();
    }

    //private helper method for adding a key to the email list
    private PrivateKey Add(string user) {
        emails.Add(user);
        return this;
    }

}

/// <summary>
/// Json serializable class for public keys
/// </summary>
class PublicKey {
    public string email { get; set; }
    public string key { get; set; }

    public PublicKey(string key, string email) {
        this.key = key;
        this.email = "";
    }

    /// <summary>
    /// retrieves the public key stored in public.key
    /// </summary>
    /// <returns>PublicKey object representing the key</returns>
    public static PublicKey RetrieveKeyInfo() {
        string keyInfo = File.ReadAllText("public.key");
        return JsonSerializer.Deserialize<PublicKey>(keyInfo);
    }

    /// <summary>
    /// retrieves a public key stored in a specified user file
    /// </summary>
    /// <param name="user">email of the user to retrieve the public key for</param>
    /// <returns>PublicKey object representing the key</returns>
    public static PublicKey GetUserKey(string user) {
        string keyInfo = File.ReadAllText(user + ".key");
        JsonDocument jsonDocument = JsonDocument.Parse(keyInfo);
        JsonElement root = jsonDocument.RootElement;
        string emailString = "";
        if (root.TryGetProperty("email", out JsonElement emailElement))
        {
            emailString = emailElement.GetString();
        }
        string keyString = "";
        if (root.TryGetProperty("key", out JsonElement keyElement))
        {
            keyString = keyElement.GetString();
        }

        return new PublicKey(keyString, emailString);
    }

    /// <summary>
    /// Saves the key to public.key
    /// </summary>
    public void WriteToFile() {
        File.WriteAllText("public.key", JsonSerializer.Serialize(this));
    }

    /// <summary>
    /// Saves the key to <paramref name="email"/>.key
    /// </summary>
    /// <param name="email">email of user to save the key for</param>
    public void WriteToFile(string email) {
        File.WriteAllText(email + ".key", JsonSerializer.Serialize(this));
    }
}

class PrimeNumbers {

    static int primeBits = 0;
    static int primeMaxCount = 0;
    static int primeCount = 0;
    static BigInteger[] primeNumbers = new BigInteger[0];

    public static BigInteger Prime(int bits) {
        primeBits = bits;
        primeMaxCount = 1;
        primeNumbers = new BigInteger[primeMaxCount];
        for(int x = 0; x < Environment.ProcessorCount * 2; x++) {
            Thread thread = new Thread(new ThreadStart(GeneratePrimes));
            thread.Start();
        }

        foreach(BigInteger num in primeNumbers) {
            if(num.IsZero) {
                primeCount = 0;
                Prime(bits);
                return primeNumbers[0];
            }
        }

        return primeNumbers[0];
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

}