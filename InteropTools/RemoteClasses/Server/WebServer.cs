﻿namespace InteropTools.RemoteClasses.Server
{
    public class WebServer
    {
        public async Task Run()
        {
            RestRouteHandler restRouteHandler = new();
            restRouteHandler.RegisterController<ParameterController>();

            HttpServerConfiguration configuration = new HttpServerConfiguration()
              .ListenOnPort(8800)
              .RegisterRoute("api", restRouteHandler)
              .EnableCors()
              .RegisterRoute(new StaticFileRouteHandler("Web"));

            HttpServer httpServer = new(configuration);
            await httpServer.StartServerAsync();

            // now make sure the app won't stop after this (eg use a BackgroundTaskDeferral)
        }
    }
}