﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace Our.Umbraco.Ditto
{
    /// <summary>
    /// The public facade for non extension method Ditto actions
    /// </summary>
    public class Ditto
    {
        /// <summary>
        /// The global context accessor type for processors.
        /// </summary>
        private static Type contextAccessorType = typeof(DefaultDittoContextAccessor);

        /// <summary>
        /// The Ditto processor attribute targets
        /// </summary>
        public const AttributeTargets ProcessorAttributeTargets = AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Enum;

        /// <summary>
        /// The default processor cache by flags
        /// </summary>
        public static DittoCacheBy DefaultCacheBy = DittoCacheBy.ContentId | DittoCacheBy.ContentVersion | DittoCacheBy.PropertyName | DittoCacheBy.Culture;

        /// <summary>
        /// The default source for umbraco property mappings
        /// </summary>
        public static PropertySource DefaultPropertySource = PropertySource.InstanceThenUmbracoProperties;

        /// <summary>
        /// The default lazy load strategy
        /// </summary>
        public static LazyLoad LazyLoadStrategy = LazyLoad.AttributedVirtuals;

        /// <summary>
        /// The property bindings for mappable properties
        /// </summary>
        internal const BindingFlags MappablePropertiesBindingFlags = BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>
        /// A list of mappable properties defined on the IPublishedContent interface
        /// </summary>
        internal static readonly IEnumerable<PropertyInfo> IPublishedContentProperties = typeof(IPublishedContent)
            .GetProperties(MappablePropertiesBindingFlags)
            .Where(x => x.IsMappable())
            .ToList();

        /// <summary>
        /// Gets a value indicating whether application is running in debug mode.
        /// </summary>
        /// <value><c>true</c> if debug mode; otherwise, <c>false</c>.</value>
        internal static bool IsDebuggingEnabled
        {
            get
            {
                try
                {
                    // Check for app setting first
                    if (!ConfigurationManager.AppSettings["Ditto:DebugEnabled"].IsNullOrWhiteSpace())
                    {
                        return ConfigurationManager.AppSettings["Ditto:DebugEnabled"].InvariantEquals("true");
                    }

                    // Check the HTTP Context
                    if (HttpContext.Current != null)
                    {
                        return HttpContext.Current.IsDebuggingEnabled;
                    }

                    // Go and get it from config directly
                    var section = ConfigurationManager.GetSection("system.web/compilation") as CompilationSection;
                    return section != null && section.Debug;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Registers a global conversion handler.
        /// </summary>
        /// <typeparam name="TObjectType">The type of the object being converted.</typeparam>
        /// <typeparam name="THandlerType">The type of the handler.</typeparam>
        public static void RegisterConversionHandler<TObjectType, THandlerType>()
            where THandlerType : DittoConversionHandler
        {
            DittoConversionHandlerRegistry.Instance.RegisterHandler<TObjectType, THandlerType>();
        }

        /// <summary>
        /// Registers the default processor type.
        /// </summary>
        /// <typeparam name="TProcessorAttributeType">The type of the processor attribute type.</typeparam>
        public static void RegisterDefaultProcessorType<TProcessorAttributeType>()
            where TProcessorAttributeType : DittoProcessorAttribute, new()
        {
            DittoProcessorRegistry.Instance.RegisterDefaultProcessorType<TProcessorAttributeType>();
        }

        /// <summary>
        /// Registers a global value resolver attribute.
        /// </summary>
        /// <typeparam name="TObjectType">The type of the object being converted.</typeparam>
        /// <typeparam name="TProcessorAttributeType">The type of the processor attribute type.</typeparam>
        public static void RegisterProcessorAttribute<TObjectType, TProcessorAttributeType>()
            where TProcessorAttributeType : DittoProcessorAttribute, new()
        {
            DittoProcessorRegistry.Instance.RegisterProcessorAttribute<TObjectType, TProcessorAttributeType>();
        }

        /// <summary>
        /// Registers a global value resolver attribute.
        /// </summary>
        /// <typeparam name="TObjectType">The type of the object being converted.</typeparam>
        /// <typeparam name="TProcessorAttributeType">The type of the processor attribute type.</typeparam>
        /// <param name="instance">An instance of the value resolver attribute to use.</param>
        public static void RegisterProcessorAttribute<TObjectType, TProcessorAttributeType>(TProcessorAttributeType instance)
            where TProcessorAttributeType : DittoProcessorAttribute
        {
            DittoProcessorRegistry.Instance.RegisterProcessorAttribute<TObjectType, TProcessorAttributeType>(instance);
        }

        /// <summary>
        /// Registers a global type converter.
        /// </summary>
        /// <typeparam name="TObjectType">The type of the object being converted.</typeparam>
        /// <typeparam name="TConverterType">The type of the converter.</typeparam>
        public static void RegisterTypeConverter<TObjectType, TConverterType>()
            where TConverterType : TypeConverter
        {
            TypeDescriptor.AddAttributes(typeof(TObjectType), new TypeConverterAttribute(typeof(TConverterType)));
        }

        /// <summary>
        /// Registers a global Ditto context accessor.
        /// </summary>
        /// <typeparam name="TDittoContextAccessorType">The type of the context accessor.</typeparam>
        public static void RegisterContextAccessor<TDittoContextAccessorType>()
            where TDittoContextAccessorType : IDittoContextAccessor, new()
        {
            contextAccessorType = typeof(TDittoContextAccessorType);
        }

        /// <summary>
        /// Gets the global umbraco application context accessor type.
        /// </summary>
        /// <returns>
        /// Returns the global umbraco application context accessor type.
        /// </returns>
        public static Type GetContextAccessorType()
        {
            return contextAccessorType;
        }

        /// <summary>
        /// Registers a processor attribute to the end of the default set of post-processor attributes.
        /// </summary>
        /// <typeparam name="TProcessorAttributeType"></typeparam>
        /// <param name="position"></param>
        public static void RegisterPostProcessorType<TProcessorAttributeType>(int position = -1)
            where TProcessorAttributeType : DittoProcessorAttribute, new()
        {
            DittoProcessorRegistry.Instance.RegisterPostProcessorType<TProcessorAttributeType>(position);
        }

        /// <summary>
        /// Deregisters a processor attribute from the default set of post-processor attributes.
        /// </summary>
        /// <typeparam name="TProcessorAttributeType"></typeparam>
        public static void DeregisterPostProcessorType<TProcessorAttributeType>()
            where TProcessorAttributeType : DittoProcessorAttribute, new()
        {
            DittoProcessorRegistry.Instance.DeregisterPostProcessorType<TProcessorAttributeType>();
        }

        /// <summary>
        /// Tries to get the associated attribute for a given object type.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="objectType">The object type.</param>
        /// <param name="attribute">The attribute.</param>
        /// <returns>Returns the associated attribute for the given type.</returns>
        public static bool TryGetAttribute<TAttribute>(Type objectType, out TAttribute attribute) where TAttribute : Attribute
        {
            if (AttributedTypeResolver<TAttribute>.HasCurrent == false)
            {
                attribute = null;
                return false;
            }

            return AttributedTypeResolver<TAttribute>.Current.TryGetAttribute(objectType, out attribute);
        }
    }
}