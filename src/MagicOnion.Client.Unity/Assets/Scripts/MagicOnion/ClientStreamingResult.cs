﻿using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UniRx;

namespace MagicOnion
{
    /// <summary>
    /// Wrapped AsyncClientStreamingCall.
    /// </summary>
    public struct ClientStreamingResult<TRequest, TResponse> :  IDisposable
    {
        readonly AsyncClientStreamingCall<byte[], byte[]> inner;
        readonly MarshallingClientStreamWriter<TRequest> requestStream;
        readonly Marshaller<TResponse> responseMarshaller;

        public ClientStreamingResult(AsyncClientStreamingCall<byte[], byte[]> inner, Marshaller<TRequest> requestMarshaller, Marshaller<TResponse> responseMarshaller)
        {
            this.inner = inner;
            this.requestStream = new MarshallingClientStreamWriter<TRequest>(inner.RequestStream, requestMarshaller);
            this.responseMarshaller = responseMarshaller;
        }

        /// <summary>
        /// Asynchronous call result.
        /// </summary>
        public IObservable<TResponse> ResponseAsync
        {
            get
            {
                var m = responseMarshaller;
                return inner.ResponseAsync.Select(x => m.Deserializer(x));
            }
        }

        public IObservable<TResponse> ResponseAsyncOnMainThread
        {
            get
            {
                return ResponseAsync.ObserveOnMainThread();
            }
        }

        /// <summary>
        /// Asynchronous access to response headers.
        /// </summary>
        public IObservable<Metadata> ResponseHeadersAsync
        {
            get
            {
                return this.inner.ResponseHeadersAsync;
            }
        }

        /// <summary>
        /// Async stream to send streaming requests.
        /// </summary>
        public IClientStreamWriter<TRequest> RequestStream
        {
            get
            {
                return this.requestStream;
            }
        }

        /// <summary>
        /// Gets the call status if the call has already finished.
        /// Throws InvalidOperationException otherwise.
        /// </summary>
        public Status GetStatus()
        {
            return this.inner.GetStatus();
        }

        /// <summary>
        /// Gets the call trailing metadata if the call has already finished.
        /// Throws InvalidOperationException otherwise.
        /// </summary>
        public Metadata GetTrailers()
        {
            return this.inner.GetTrailers();
        }

        /// <summary>
        /// Provides means to cleanup after the call.
        /// If the call has already finished normally (request stream has been completed and call result has been received), doesn't do anything.
        /// Otherwise, requests cancellation of the call which should terminate all pending async operations associated with the call.
        /// As a result, all resources being used by the call should be released eventually.
        /// </summary>
        /// <remarks>
        /// Normally, there is no need for you to dispose the call unless you want to utilize the
        /// "Cancel" semantics of invoking <c>Dispose</c>.
        /// </remarks>
        public void Dispose()
        {
            this.inner.Dispose();
        }
    }
}