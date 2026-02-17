using NSec.Cryptography;
using System.Net.Http.Json;
using System.Text.Json;

// load private key
var privateKeyBase64 = File.ReadAllText("private.key");

var privateKey = Key.Import(
    SignatureAlgorithm.Ed25519,
    Convert.FromBase64String(privateKeyBase64),
    KeyBlobFormat.RawPrivateKey);

var client = new HttpClient();


// STEP 1: request challenge

var challengeResponse = await client.PostAsync(
    "http://localhost:5266/auth/request?username=test",
    null);

var challengeJson =
    await challengeResponse.Content.ReadAsStringAsync();

Console.WriteLine("Challenge response:");
Console.WriteLine(challengeJson);


// parse JSON

var challengeData =
    JsonSerializer.Deserialize<ChallengeResponse>(challengeJson);

if (challengeData == null)
{
    Console.WriteLine("Failed to parse challenge");
    return;
}


// STEP 2: sign challenge

var challengeBytes =
    Convert.FromBase64String(challengeData.challenge);

var signature =
    SignatureAlgorithm.Ed25519.Sign(
        privateKey,
        challengeBytes);

var signatureBase64 =
    Convert.ToBase64String(signature);


// STEP 3: verify login

var verifyResponse = await client.PostAsJsonAsync(
    "http://localhost:5266/auth/verify",
    new VerifyRequest
    {
        ChallengeId = challengeData.challengeId,
        SignatureBase64 = signatureBase64
    });

var result =
    await verifyResponse.Content.ReadAsStringAsync();

Console.WriteLine();
Console.WriteLine("Login result:");
Console.WriteLine(result);



// models

public class ChallengeResponse
{
    public Guid challengeId { get; set; }

    public string challenge { get; set; } = "";
}

public class VerifyRequest
{
    public Guid ChallengeId { get; set; }

    public string SignatureBase64 { get; set; } = "";
}
