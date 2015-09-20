Known issues {#known_issues_ref}
===========

There are a few use cases that we know to be causing issues, which are usually due to broken functionality in Unity. If you are experiencing strange behaviour, please check out the following list first.

## HTTP request freezing on second run from the editor

This bug has been reproduced on Unity 4 on MS Windows (8.1). It is currently unknown whether it affects subsequent or previous versions of either the operating system or Unity software, but it does not affect the Mac OS X build of Unity 5 at least.

Any request (such as login) will freeze upon second run of a scene just after having launched Unity. Retrying the request will then work immediately. Subsequent requests and runs of the scene also work. In order to reproduce the issue, you need to quit Unity, launch the scene, stop it, then launch it again; the first request will then time out.

This bug has been reported in the official bug tracker at Unity.

## ERROR building certificate chain on iOS

The following warning might be outputted on iOS only during the first request.

~~~~
ERROR building certificate chain: System.ArgumentException: certificate ---> System.Security.Cryptography.CryptographicException: Unsupported hash algorithm: 1.2.840.113549.1.1.12
  at Mono.Security.X509.X509Certificate.VerifySignature (System.Security.Cryptography.RSA rsa) [0x00000] in <filename unknown>:0 
  at Mono.Security.X509.X509Certificate.VerifySignature (System.Security.Cryptography.AsymmetricAlgorithm aa) [0x00000] in <filename unknown>:0 
  at System.Security.Cryptography.X509Certificates.X509Chain.IsSignedWith (System.Security.Cryptography.X509Certificates.X509Certificate2 signed, System.Security.Cryptography.AsymmetricAlgorithm pubkey) [0x00000] in <filename unknown>:0 
  at System.Security.Cryptography.X509Certificates.X509Chain.Process (Int32 n) [0x00000] in <filename unknown>:0 
  at System.Security.Cryptography.X509Certificates.X509Chain.ValidateChain (X509ChainStatusFlags flag) [0x00000] in <filename unknown>:0 
  at System.Security.Cryptography.X509Certificates.X509Chain.Build (System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) [0x00000] in <filename unknown>:0 
  --- End of inner exception stack trace ---
  at System.Security.Cryptography.X509Certificates.X509Chain.Build (System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) [0x00000] in <filename unknown>:0 
  at System.Net.ServicePointManager+ChainValidationHelper.ValidateChain (Mono.Security.X509.X509CertificateCollection certs) [0x00000] in <filename unknown>:0 
Please, report this problem to the Mono team
~~~~

This error is due to the implementation of HTTPS on Unity iOS which does not support secure enough protocols. We are currently trying to work out this issue and have submitted a bug report.
