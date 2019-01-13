﻿using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Common.Options;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Rags;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public class InfoController : IHttpController,
                                  IHandle<SystemMessage.StateChangeMessage>
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<InfoController>();
        private static readonly ICodec[] SupportedCodecs = { Codec.Json, Codec.Xml, Codec.ApplicationXml, Codec.Text };

        private readonly IOptions _options;
        private readonly ProjectionType _projectionType;
        private VNodeState _currentState;

        public InfoController(IOptions options, ProjectionType projectionType)
        {
            _options = options;
            _projectionType = projectionType;
        }

        public void Subscribe(IHttpService service)
        {
            if (null == service) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.service); }
            service.RegisterAction(new ControllerAction("/info", HttpMethod.Get, Codec.NoCodecs, SupportedCodecs), OnGetInfo);
            service.RegisterAction(new ControllerAction("/info/options", HttpMethod.Get, Codec.NoCodecs, SupportedCodecs), OnGetOptions);
        }


        public void Handle(SystemMessage.StateChangeMessage message)
        {
            _currentState = message.State;
        }

        private void OnGetInfo(HttpEntityManager entity, UriTemplateMatch match)
        {
            entity.ReplyTextContent(Codec.Json.To(new
            {
                ESVersion = VersionInfo.Version,
                // ## 苦竹 修改 ##
                //State = _currentState.ToString().ToLower(),
                State = VNodeStateHelper.ConvertToLower(_currentState),
                ProjectionsMode = _projectionType
            }),
            HttpStatusCode.OK,
            "OK",
            entity.ResponseCodec.ContentType,
            null,
            e => Log.LogError(e, "Error while writing HTTP response (info)"));
        }

        private void OnGetOptions(HttpEntityManager entity, UriTemplateMatch match)
        {
            if (entity.User != null && entity.User.IsInRole(SystemRoles.Admins))
            {
                entity.ReplyTextContent(Codec.Json.To(Filter(GetOptionsInfo(_options), new[] { "CertificatePassword" })),
                                        HttpStatusCode.OK,
                                        "OK",
                                        entity.ResponseCodec.ContentType,
                                        null,
                                        e => Log.LogError(e, "error while writing HTTP response (options)"));
            }
            else
            {
                entity.ReplyStatus(HttpStatusCode.Unauthorized, "Unauthorized", LogReplyError);
            }
        }

        private void LogReplyError(Exception exc)
        {
            if (Log.IsDebugLevelEnabled()) Log.LogDebug("Error while replying (info controller): {0}.", exc.Message);
        }

        public class OptionStructure
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Group { get; set; }
            public string Value { get; set; }
            public string[] PossibleValues { get; set; }
        }

        public OptionStructure[] GetOptionsInfo(IOptions options)
        {
            var optionsToSendToClient = new List<OptionStructure>();
            foreach (var property in options.GetType().GetProperties())
            {
                var argumentDescriptionAttribute = property.HasAttr<ArgDescriptionAttribute>() ? property.Attr<ArgDescriptionAttribute>() : null;
                var configFileOptionValue = property.GetValue(options, null);
                string[] possibleValues = null;
                if (property.PropertyType.IsEnum)
                {
                    possibleValues = property.PropertyType.GetEnumNames();
                }
                else if (property.PropertyType.IsArray)
                {
                    var array = configFileOptionValue as Array;
                    if (array == null) { continue; }
                    var configFileOptionValueAsString = String.Empty;
                    for (var i = 0; i < array.Length; i++)
                    {
                        configFileOptionValueAsString += array.GetValue(i).ToString();
                    }
                    configFileOptionValue = configFileOptionValueAsString;
                }
                optionsToSendToClient.Add(new OptionStructure
                {
                    Name = property.Name,
                    Description = argumentDescriptionAttribute == null ? "" : argumentDescriptionAttribute.Description,
                    Group = argumentDescriptionAttribute == null ? "" : argumentDescriptionAttribute.Group,
                    Value = configFileOptionValue == null ? "" : configFileOptionValue.ToString(),
                    PossibleValues = possibleValues
                });
            }
            return optionsToSendToClient.ToArray();
        }
        public OptionStructure[] Filter(OptionStructure[] optionsToBeFiltered, params string[] namesOfValuesToExclude)
        {
            return optionsToBeFiltered.Select(x =>
                    new OptionStructure
                    {
                        Name = x.Name,
                        Description = x.Description,
                        Group = x.Group,
                        PossibleValues = x.PossibleValues,
                        Value = namesOfValuesToExclude.Contains(y => y.Equals(x.Name, StringComparison.OrdinalIgnoreCase)) ? String.Empty : x.Value
                    }).ToArray();
        }
    }
}
