﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TypeCobol.Analysis {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resource() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("TypeCobol.Analysis.Resource", typeof(Resource).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
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
        ///   Looks up a localized string similar to Bad ALTER instruction: could not locate corresponding altered GO TO..
        /// </summary>
        internal static string BadAlterIntrWithNoSiblingGotoInstr {
            get {
                return ResourceManager.GetString("BadAlterIntrWithNoSiblingGotoInstr", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PERFORM &apos;{0}&apos; THRU &apos;{1}&apos;: &apos;{1}&apos; is declared before &apos;{0}&apos;..
        /// </summary>
        internal static string BadPerformProcedureThru {
            get {
                return ResourceManager.GetString("BadPerformProcedureThru", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Statement &apos;{0}&apos; located at line {1}, column {2} prevents this PERFORM statement to reach its exit..
        /// </summary>
        internal static string BasicBlockGroupGoesBeyondTheLimit {
            get {
                return ResourceManager.GetString("BasicBlockGroupGoesBeyondTheLimit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A recursive loop has been encountered while analyzing &apos;PERFORM {0}&apos;, recursive instruction is &apos;{1}&apos;..
        /// </summary>
        internal static string RecursiveBlockOnPerformProcedure {
            get {
                return ResourceManager.GetString("RecursiveBlockOnPerformProcedure", resourceCulture);
            }
        }
    }
}
