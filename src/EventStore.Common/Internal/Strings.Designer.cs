﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace EventStore.Common.Internal {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("EventStore.Common.Internal.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   重写当前线程的 CurrentUICulture 属性
        ///   重写当前线程的 CurrentUICulture 属性。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 Compiler cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_Compiler {
            get {
                return ResourceManager.GetString("ArgumentNull_Compiler", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Type cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_Type {
            get {
                return ResourceManager.GetString("ArgumentNull_Type", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 The partial action has been built. No handler can be added after that. 的本地化字符串。
        /// </summary>
        internal static string InvalidOperation_MatchBuilder_Built {
            get {
                return ResourceManager.GetString("InvalidOperation_MatchBuilder_Built", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 A handler that catches all messages has been added. No handler can be added after that. 的本地化字符串。
        /// </summary>
        internal static string InvalidOperation_MatchBuilder_MatchAnyAdded {
            get {
                return ResourceManager.GetString("InvalidOperation_MatchBuilder_MatchAnyAdded", resourceCulture);
            }
        }
    }
}
