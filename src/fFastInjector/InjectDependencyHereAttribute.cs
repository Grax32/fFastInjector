/*
New BSD License (BSD)
Copyright (c) 2019, David Walker (https://www.grax.com/fFastInjector/)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that
the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the
following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System;

namespace fFastInjector
{
    /// <summary>
    /// <para>This attribute is used by the auto-resolver to indicate that a dependency should be inserted.</para>
    /// <para>When attached to a constructor, that constructor will be selected by the auto-resolver.</para>
    /// <para>When attached to a method, that method will be called by the auto-resolver and each of the arguments will be fulfilled by the auto-resolver.
    /// i.e. MyClass.InjectStuff(IRepository repo) would be called with MyClass.InjectStuff(Injector.Resolve&lt;IRepository&gt;())</para>
    /// <para>When attached to a property or field, the dependency will be injected by setting the property/field to the result of a Resolve call.
    /// i.e. MyClass.MyInjectedRepositoryProperty = Injector.Resolve&lt;IRepository&gt;()</para>
    /// </summary>
    /// <example>
    /// <para>
    /// When attached to a constructor, this attribute will cause the auto-resolver to selected that constructor over any other.  In the example, the
    /// constructor with 2 parameters would be selected.
    /// <code source="..\DocumentationSampleCode\InjectDependencyHereAttributeUsage.cs" language="cs" region="Constructors" title="Constructor Usage"/>
    /// If the auto-resolver finds this attribute on more than one constructor, the auto-resolved will select one (it is undefined as to which one will be picked) and no error will be thrown.
    /// </para>
    /// </example>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class InjectDependencyHereAttribute : Attribute { }
}
