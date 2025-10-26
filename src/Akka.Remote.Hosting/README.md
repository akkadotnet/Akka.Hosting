# Akka Remoting Akka.Hosting Extensions

## WithRemoting() Method

An extension method to add [Akka.Remote](https://getakka.net/articles/remoting/index.html) support to the `ActorSystem`.

```csharp
public static AkkaConfigurationBuilder WithRemoting(
    this AkkaConfigurationBuilder builder,
    string hostname = null,
    int? port = null,
    string publicHostname = null,
    int? publicPort = null);
```

### Parameters
* `hostname` __string__

  Optional. The hostname to bind Akka.Remote upon.

  __Default__: `IPAddress.Any` or "0.0.0.0"

* `port` __int?__

  Optional. The port to bind Akka.Remote upon.

  __Default__: 2552

* `publicHostname` __string__

  Optional. If using hostname aliasing, this is the host we will advertise.

  __Default__: Fallback to `hostname`

* `publicPort` __int?__

  Optional. If using port aliasing, this is the port we will advertise.

  __Default__: Fallback to `port`

### Example

```csharp
using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("remotingDemo", (builder, provider) =>
        {
            builder.WithRemoting("127.0.0.1", 4053);
        });
    }).Build();

await host.RunAsync();
```

## SSL/TLS Configuration

Akka.Remote supports SSL/TLS encryption for secure communication between actor systems. Starting with Akka.NET v1.5.55, enhanced certificate validation options are available.

### Basic SSL Configuration with Certificate File

```csharp
using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("secureSystem", (builder, provider) =>
        {
            builder.WithRemoting(options =>
            {
                options.HostName = "127.0.0.1";
                options.Port = 4053;
                options.EnableSsl = true;
                options.Ssl.CertificateOptions.Path = "/path/to/certificate.pfx";
                options.Ssl.CertificateOptions.Password = "certificate-password";
                options.Ssl.SuppressValidation = false;
                options.Ssl.RequireMutualAuthentication = true;
                options.Ssl.ValidateCertificateHostname = false;
            });
        });
    }).Build();

await host.RunAsync();
```

### SSL Configuration with X509Certificate2

```csharp
using System.Security.Cryptography.X509Certificates;

var certificate = new X509Certificate2("/path/to/certificate.pfx", "certificate-password");

using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("secureSystem", (builder, provider) =>
        {
            builder.WithRemoting(options =>
            {
                options.HostName = "127.0.0.1";
                options.Port = 4053;
                options.EnableSsl = true;
                options.Ssl.X509Certificate = certificate;
                options.Ssl.SuppressValidation = false;
                options.Ssl.RequireMutualAuthentication = true;
                options.Ssl.ValidateCertificateHostname = false;
            });
        });
    }).Build();

await host.RunAsync();
```

### Advanced: Custom Certificate Validation (Akka.NET v1.5.55+)

For advanced security scenarios, you can provide a custom certificate validation callback. This is useful for:
- Certificate pinning
- Subject/Issuer validation
- Custom trust store validation
- Business-specific validation rules

```csharp
using System.Security.Cryptography.X509Certificates;
using Akka.Remote.Transport.DotNetty;

var certificate = new X509Certificate2("/path/to/certificate.pfx", "certificate-password");

using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("secureSystem", (builder, provider) =>
        {
            builder.WithRemoting(options =>
            {
                options.HostName = "127.0.0.1";
                options.Port = 4053;
                options.EnableSsl = true;
                options.Ssl.X509Certificate = certificate;

                // Example: Certificate pinning - only accept certificates with specific thumbprint
                options.Ssl.CustomValidator = (cert, chain, peer, errors, log) =>
                {
                    if (cert == null)
                    {
                        log.Warning("Peer {0} presented no certificate", peer);
                        return false;
                    }

                    var expectedThumbprint = "YOUR_EXPECTED_THUMBPRINT_HERE";
                    var isValid = cert.Thumbprint.Equals(expectedThumbprint, StringComparison.OrdinalIgnoreCase);

                    if (!isValid)
                    {
                        log.Warning("Peer {0} presented certificate with thumbprint {1}, expected {2}",
                            peer, cert.Thumbprint, expectedThumbprint);
                    }

                    return isValid;
                };
            });
        });
    }).Build();

await host.RunAsync();
```

### Using CertificateValidation Helper Methods (Akka.NET v1.5.55+)

Akka.NET v1.5.55 provides helper methods in the `CertificateValidation` class for common validation scenarios:

```csharp
using System.Security.Cryptography.X509Certificates;
using Akka.Remote.Transport.DotNetty;

var certificate = new X509Certificate2("/path/to/certificate.pfx", "certificate-password");

using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("secureSystem", (builder, provider) =>
        {
            builder.WithRemoting(options =>
            {
                options.HostName = "127.0.0.1";
                options.Port = 4053;
                options.EnableSsl = true;
                options.Ssl.X509Certificate = certificate;

                // Example 1: Validate certificate chain + hostname
                options.Ssl.CustomValidator = CertificateValidation.Combine(
                    CertificateValidation.ValidateChain(),
                    CertificateValidation.ValidateHostname()
                );

                // Example 2: Pin specific certificate
                options.Ssl.CustomValidator = CertificateValidation.PinnedCertificate(certificate);

                // Example 3: Validate subject matches expected pattern
                options.Ssl.CustomValidator = CertificateValidation.ValidateSubject("CN=*.mycompany.com");

                // Example 4: Validate issuer
                options.Ssl.CustomValidator = CertificateValidation.ValidateIssuer("CN=My Company Root CA");

                // Example 5: Combine multiple validators - chain validation + subject validation
                options.Ssl.CustomValidator = CertificateValidation.Combine(
                    CertificateValidation.ValidateChain(),
                    CertificateValidation.ValidateSubject("CN=*.mycompany.com")
                );
            });
        });
    }).Build();

await host.RunAsync();
```

### SSL Configuration Options

- **`EnableSsl`** (bool?): Enable/disable SSL/TLS encryption
- **`SuppressValidation`** (bool?): Suppress all certificate validation (NOT recommended for production)
- **`RequireMutualAuthentication`** (bool?): Require both client and server to present valid certificates (default: true as of v1.5.52)
- **`ValidateCertificateHostname`** (bool?): Validate certificate hostname matches target (default: false as of v1.5.53)
- **`CustomValidator`** (CertificateValidationCallback?): Custom certificate validation logic (available in v1.5.55+)
- **`X509Certificate`** (X509Certificate2?): Certificate to use for SSL/TLS

**Note:** When `X509Certificate` is provided, Akka.Remote uses `DotNettySslSetup` for programmatic configuration which takes precedence over HOCON configuration. If you need HOCON-based SSL configuration, use `CertificateOptions` instead of providing an `X509Certificate` object.
