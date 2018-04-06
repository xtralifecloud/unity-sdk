using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Disable gzip compression on Universal Windows Platform
#if !WINDOWS_UWP
// Use Ionic.Zlib's GZipStream instead of System.IO.Compression's one to handle gzip compression on none-Windows platforms
//using System.IO.Compression;
using Ionic.Zlib;
#endif

namespace CotcSdk {
	/**
	 * Better HTTP client based on UnityEngine.Networking.UnityWebRequest.
	 */
	internal class UnityHttpClientV2 : HttpClient {

		/// <summary>Asynchronous request state.</summary>
		private class UnityRequest : WebRequest {
			// This class stores the State of the request.
			private string ContentType;
			public UnityHttpClientV2 self;
			private UnityWebRequest Request;
			private bool WasAborted = false;

			public UnityRequest(UnityHttpClientV2 inst, string url, HttpRequest request, object previousUserData, int requestId) : base(inst, request) {
                self = inst;
                OriginalRequest = request;
                RequestId = requestId;
                PreviousUserData = previousUserData;

                Request = new UnityWebRequest(url);
                // Auto-choose HTTP method
                Request.method = request.Method ?? (request.Body != null ? "POST" : "GET");

				// Disable gzip compression on Universal Windows Platform
				#if !WINDOWS_UWP
				request.Headers["Accept-Encoding"] = "gzip";
				#endif

				// TODO Missing functionality (currently unsupported by UnityWebRequest).
				//				req.SetRequestHeader("User-agent", request.UserAgent);
				//				req.keepAlive = true;
                foreach (var pair in request.Headers)
                    Request.SetRequestHeader(pair.Key, pair.Value);

                if (OriginalRequest.Body != null)
                {
                    UploadHandler uploader = new UploadHandlerRaw(OriginalRequest.Body);
                    if (ContentType != null) uploader.contentType = ContentType;
                    Request.uploadHandler = uploader;
                }
                Request.downloadHandler = new DownloadHandlerBuffer();
			}

			public override void AbortRequest() {
				WasAborted = true;
				if (Request != null) {
                    Request.Abort();
				}
			}

			/// <summary>Prints the current request for user convenience.</summary>
			internal void LogRequest() {
				if (!VerboseMode) { return; }

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("[" + RequestId + "] " + Request.method + "ing on " + Request.url);
				sb.AppendLine("Headers (Unity):");
				foreach (var pair in OriginalRequest.Headers) {
					sb.AppendLine("\t" + pair.Key + ": " + pair.Value);
				}
				if (OriginalRequest.HasBody) {
					sb.AppendLine("Body: " + OriginalRequest.BodyString);
				}
				Common.Log(sb.ToString());
			}

			/// <summary>Prints information about the response for debugging purposes.</summary>
			internal void LogResponse(HttpResponse response) {
				if (!VerboseMode) { return; }

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("[" + RequestId + "] " + response.StatusCode + " on " + Request.method + "ed on " + Request.url);
				sb.AppendLine("Recv. headers:");
				foreach (var pair in response.Headers) {
					if (String.Compare(pair.Key, "Content-Type", true) == 0) {
						ContentType = pair.Value;
					}
					else {
						sb.AppendLine("\t" + pair.Key + ": " + pair.Value);
					}
				}
				if (response.HasBody) {
					sb.AppendLine("Body: " + response.BodyString);
				}
				Common.Log(sb.ToString());
			}

            private IEnumerator ProcessRequest(UnityWebRequest req) {
                yield return req.Send();

                bool processRequest = true;
                if (req.isError)
                {
                    // Nice bug again, in the iOS UnityWebRequest implementation. If server returns a 400 code following a
                    // a successful request process, req.isError is flagged to true... Not sure at this stage how many HTTP
                    // return codes are considered as errors.
                    if (!(Application.platform == RuntimePlatform.IPhonePlayer && Request.responseCode == 400))
                    {
                        processRequest = false;
                        if (!WasAborted)
                        {
                            string errorMessage = "Failed web request: " + req.error + " - Status code: " + Request.responseCode;
                            Common.Log(errorMessage);
                            self.FinishWithRequest(this, new HttpResponse(new Exception(errorMessage)));
                        }
                        else
                        {
                            Common.Log("Request aborted");
                        }
                    }
                }
                
                if(processRequest)
                {
                    // Extracts asset bundle
                    HttpResponse response = new HttpResponse();
                    response.StatusCode = (int)Request.responseCode;
                    Common.Log("ProcessRequest - response code: " + Request.responseCode);
                    if (Application.platform == RuntimePlatform.IPhonePlayer && response.StatusCode <= 0)
                    {
                        // Nice bug in the iOS UnityWebRequest implementation. When you abort a request, in fact it doesn't
                        // abort the request. Instead of receiving an error (req.isError and req.error can check that), you
                        // just receive a funny responseCode (like 0 or -1001) and an empty dictionary of response headers.
                        Common.Log("Request aborted - Unity iOS bug for UnityWebRequest...");
                    }
                    else
                    {
						Dictionary<string, string> responseHeaders = Request.GetResponseHeaders();

						if (responseHeaders != null)
						{
                            // Disable gzip compression on Universal Windows Platform
                            #if WINDOWS_UWP
								response.Body = Request.downloadHandler.data;
                            #else
                            if (responseHeaders.ContainsKey("Content-Encoding") && (responseHeaders["Content-Encoding"] == "gzip"))
								response.Body = GzipDecompress(Request.downloadHandler.data);
							else
								response.Body = Request.downloadHandler.data;
                            #endif

                            foreach (var pair in responseHeaders)
                                response.Headers[pair.Key] = pair.Value;
						}
                        else
                            Common.Log("Empty header response, should not be the case.");
						
                        LogResponse(response);
                        self.FinishWithRequest(this, response);
                    }
                }
            }

            // Disable gzip compression on Universal Windows Platform
            #if !WINDOWS_UWP
            private byte[] GzipDecompress(byte[] data)
			{
				MemoryStream compressedData = new MemoryStream(data);
				MemoryStream decompressedData = new MemoryStream();

				GZipStream decompress = new GZipStream(compressedData, CompressionMode.Decompress);

				byte[] buffer = new byte[4096];
				int numRead;

				while ((numRead = decompress.Read(buffer, 0, buffer.Length)) != 0)
					decompressedData.Write(buffer, 0, numRead);

				return decompressedData.ToArray();
			}
            #endif

            public override void Start() {
				// Configure & perform the request
				LogRequest();
				Cotc.RunCoroutine(ProcessRequest(Request));
			}
		}

		protected override WebRequest CreateRequest(HttpRequest request, string url, object previousUserData) {
			return new UnityRequest(this, url, request, previousUserData, (RequestCount += 1));
		}
	}
}
