// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.SignalR
{
    public interface IUserIdProvider
    {
        string GetUserId(HttpContext context);
    }
}