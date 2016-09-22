using UnityEngine;
using System;
using System.Collections;
using System.Text;

namespace CotcSdk {
	public class DomainEventLoopCoroutine : MonoBehaviour {
		private DomainEventLoop domainEventLoop;
		private int delay;
		private string messageToAcknowledge;
		private bool lastResultPositive = true;

		public void StartEventLoopCoroutine(DomainEventLoop del = null) {
			if (del != null) {
				domainEventLoop = del;
				delay = del.LoopIterationDuration;
			}
			StartCoroutine("EventLoopCoroutine");
		}

		public void StopEventLoopCoroutine() {
			StopCoroutine("EventLoopCoroutine");
		}

		private IEnumerator EventLoopCoroutine() {
			// In case of stop, prevent to continue the coroutine if DomainEventLoop hasn't (shouldn't happen)
			while (!domainEventLoop.Stopped) {
				if (!lastResultPositive) {
					// Last time failed, wait a bit to avoid bombing the Internet.
					yield return new WaitForSeconds(DomainEventLoop.PopEventDelayCoroutineHold);
				}

				UrlBuilder url = new UrlBuilder("/v1/gamer/event");
				url.Path(domainEventLoop.Domain).QueryParam("timeout", delay);
				if (messageToAcknowledge != null) {
					url.QueryParam("ack", messageToAcknowledge);
				}
				
				domainEventLoop.CurrentRequest = domainEventLoop.Gamer.MakeHttpRequest(url);
				domainEventLoop.CurrentRequest.RetryPolicy = HttpRequest.Policy.NonpermanentErrors;
				domainEventLoop.CurrentRequest.TimeoutMillisec = delay + 30000;
				domainEventLoop.CurrentRequest.DoNotEnqueue = true;
				
				Managers.HttpClient.Run(domainEventLoop.CurrentRequest, (HttpResponse res) => {
					domainEventLoop.CurrentRequest = null;
					try {
						lastResultPositive = true;
						if (res.StatusCode == 200) {
							messageToAcknowledge = res.BodyJson["id"];
							ProcessEvent(res);
						} else if (res.StatusCode != 204) {
							lastResultPositive = false;
							// Non retriable error -> kill ourselves
							if (res.StatusCode >= 400 && res.StatusCode < 500) {
								domainEventLoop.Stopped = true;
							}
						}
					} catch (Exception e) {
						Common.LogError("Exception happened in pop event loop: " + e.ToString());
					}
					// Start a new loop after response or timeout are processed if not paused or stoped
					StopEventLoopCoroutine();
					StartEventLoopCoroutine();
				});
				
				// After timeout is reached but not processed, continue to a new loop if the HttpResponse's delegate hasn't (shouldn't happen)
				yield return new WaitForSeconds(domainEventLoop.CurrentRequest.TimeoutMillisec + 10000);
			}
			
			yield return null;
		}

		private void ProcessEvent(HttpResponse res) {
			try {
				EventLoopArgs args = new EventLoopArgs(res.BodyJson);
				if (domainEventLoop.receivedEvent != null) domainEventLoop.receivedEvent(domainEventLoop, args);
				Cotc.NotifyReceivedMessage(domainEventLoop, args);
			}
			catch (Exception e) {
				Common.LogError("Exception in the event chain: " + e.ToString());
			}
		}
	}
}
