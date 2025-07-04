﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Cdm.Authentication.Browser
{
    /// <summary>
    /// OAuth 2.0 verification browser that runs a local server and waits for a call with
    /// the authorization verification code.
    /// </summary>
    public class StandaloneBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;

        private const string _closePageResponse = @"
        <!DOCTYPE html>
        <html lang=""en"">
        <head>
            <title>Sign in with Sphere</title>
            <meta name=""description"" content=""Log in with your Sphere Account to access your games."">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
            <link rel=""icon"" href=""data:image/x-icon;base64,AAABAAMAEBAAAAEAIABoBAAANgAAACAgAAABACAAKBEAAJ4EAAAwMAAAAQAgAGgmAADGFQAAKAAAABAAAAAgAAAAAQAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv7+/BNLS0jnW1tZr1dXVe9bW1l7U1NQqf39/AgAAAAAAAAAA19fXDQAAAAAAAAAAAAAAAAAAAADW1tYm1dXVldXV1ezW1NX/18vO/9bS0//W1tb/1tbW/9bW1vPV1dXX1tbW2tbW1voAAAAAAAAAAAAAAADV1dV01tXV+9q4vv/gk6P/5XiQ/+V1jv/jg5j/18zP/9bW1v/W1tb/1tbW/9bW1v/W1tb/AAAAAAAAAAAAAAAA1dXVH+KLk8/mfIj/5nyI/9yxt//W1NT/1tbW/9bW1v/W1tb/1tbW/9bW1v/V1dX+1dXVxQAAAAAAAAAAAAAAAOWEfzLnhIH/54SB/+eEgf/ljYn/45aS/+KYk//gop3/3Li2/9fPzdrW1tac2NjYLgAAAAAAAAAAAAAAAAAAAADnjnlt5456/+eOev/njnr/5456/+eOev/njnr/5456/+eOev/njnltAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA6ZhybeiYcv/omHL/6Jhy/+iYcv/omHL/6Jhy/+iYcv/omHL/6ZhybQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAOqjazLoomr/6KJq/+iiav/oomr/6KJq/+iiav/oomv/6KJq/+qjazIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA6apjr+mrY//pq2P/6atj/+mrY//pq2P/6atj/+mqY68AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAOmqVQzpsV2v6bJd/+myXf/psl3/6bJd/+mxXa/pqlUMAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAOq3WzLpuFtt6bhbbeq3WzIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAAAACAAAABAAAAAAQAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA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AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAAAADAAAABgAAAAAQAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD///8Cz8/PG9TU1DzU1NRa1tbWcNbW1n3V1dV71dXVb9TU1FvV1dU+1NTUHv///wIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA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AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="">
            <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
            <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
            <link href=""https://fonts.googleapis.com/css2?family=Poppins:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&display=swap"" rel=""stylesheet"">
            <link href=""https://fonts.googleapis.com/icon?family=Material+Icons+Outlined"" rel=""stylesheet"">
        </head>
        <body>
            <style>
                body {
                    box-sizing: border-box;
                    min-height: 100dvh;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    justify-content: center;
                    font-family: 'Poppins', sans-serif;
                    margin: 0;
                    padding: 1.25rem;
                    color: #e7e0eb;
                    background-color: #1a1524;
                }

                @media (min-width: 640px) {
                    body {
                        padding: 2.5rem;
                    }
                }

                div.main-content {
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    justify-content: center;
                    height: 24rem;
                }

                span.primary-icon {
                    font-size: 6rem;
                    color: #1a692c;
                    margin: 0 auto 0.5rem;
                }

                @media (prefers-color-scheme: dark) {
                    span.primary-icon {
                        color: #9bf285; 
                    }
                }

                @media (min-width: 640px) {
                    span.primary-icon {
                        font-size: 8rem;
                    }
                }

                p {
                    margin: 0;
                }

                p.header {
                    text-align: center;
                    font-size: 1.5rem;
                    line-height: 2rem;
                    font-weight: 600;
                    margin-bottom: 1.25rem;
                }

                p.description {
                    text-align: center;
                    font-size: 1rem;
                    line-height: 1.5rem;
                }
            </style>
            <div class=""main-content"">
                <span class=""material-icons-outlined primary-icon"">done</span>
                <p class=""header"">You're signed in!</p>
                <p class=""description"">You may close this tab now.</p>
            </div>
        </body>
        </html>
        ";

        private string _loginOrigin;
        private string _allowedOrigin;

        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, CancellationToken cancellationToken = default,
            bool internalDevelopmentMode = false)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            cancellationToken.Register(() => { _taskCompletionSource?.TrySetCanceled(); });

            using var httpListener = new HttpListener();

            try
            {
                _loginOrigin = new Uri(loginUrl).GetLeftPart(UriPartial.Authority);
                _allowedOrigin = internalDevelopmentMode ? "http://127.0.0.1:3100" : "https://login.sphereapp.co";
                redirectUrl = AddForwardSlashIfNecessary(redirectUrl);
                httpListener.Prefixes.Add(redirectUrl);
                httpListener.Start();
                httpListener.BeginGetContext(IncomingHttpRequest, httpListener);

                Application.OpenURL(loginUrl);

                return await _taskCompletionSource.Task;
            }
            finally
            {
                httpListener.Stop();
            }
        }

        private void IncomingHttpRequest(IAsyncResult result)
        {
            var httpListener = (HttpListener)result.AsyncState;
            var httpContext = httpListener.EndGetContext(result);
            var httpRequest = httpContext.Request;
            var httpResponse = httpContext.Response;
            httpResponse.AddHeader("Access-Control-Allow-Origin", _allowedOrigin);
            httpResponse.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");

            if (httpRequest.HttpMethod == "OPTIONS")
            {
                httpResponse.StatusCode = 200;
                httpResponse.ContentLength64 = 0;
                httpResponse.OutputStream.Close();
            }
            else
            {
                // Build a response to send an "ok" back to the browser for the user to see.
                var buffer = System.Text.Encoding.UTF8.GetBytes(_closePageResponse);

                // Send the output to the client browser.
                httpResponse.ContentLength64 = buffer.Length;
                var output = httpResponse.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();

                _taskCompletionSource.SetResult(
                    new BrowserResult(BrowserStatus.Success, httpRequest.Url.ToString()));
            }
        }

        /// <summary>
        /// Prefixes must end in a forward slash ("/")
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=net-7.0#remarks" />
        private string AddForwardSlashIfNecessary(string url)
        {
            var forwardSlash = "/";
            if (!url.EndsWith(forwardSlash)) url += forwardSlash;

            return url;
        }
    }
}