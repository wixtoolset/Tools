// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.WixCop
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;
    using WixToolset.Tools.WixCop;
    using Xunit;

    public class ConverterFixture
    {
        private static readonly XNamespace Wix4Namespace = "http://wixtoolset.org/schemas/v4/wxs";

        [Fact]
        public void EnsuresDeclaration()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsuresUtf8Declaration()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "    <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 4, null, null);

            var errors = converter.ConvertDocument(document);

            Assert.Equal(1, errors);
            Assert.Equal("1.0", document.Declaration.Version);
            Assert.Equal("utf-8", document.Declaration.Encoding);
        }

        [Fact]
        public void CanFixWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'",
                "              Value='Val'>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "    </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 4, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(4, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanFixCdataWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'>",
                "       <![CDATA[1<2]]>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\"><![CDATA[1<2]]></Property>",
                "  </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(2, errors);
        }

        [Fact]
        public void CanFixCdataWithWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'>",
                "       <![CDATA[",
                "           1<2",
                "       ]]>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\"><![CDATA[1<2]]></Property>",
                "  </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(2, errors);
        }

        [Fact]
        public void CanConvertMainNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            //Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertNamedMainNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<w:Wix xmlns:w='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <w:Fragment />",
                "</w:Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <w:Fragment />",
                "</w:Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetNamespaceOfPrefix("w"));
        }

        [Fact]
        public void CanConvertNonWixDefaultNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<w:Wix xmlns:w='http://schemas.microsoft.com/wix/2006/wi' xmlns='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(2, errors);
            Assert.Equal(Wix4Namespace, document.Root.GetNamespaceOfPrefix("w"));
            Assert.Equal("http://wixtoolset.org/schemas/v4/wxs/util", document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertExtensionNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertMissingNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertAnonymousFile()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <File Source='path\\to\\foo.txt' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <File Id=\"foo.txt\" Source=\"path\\to\\foo.txt\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationNo()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <MsiPackage SuppressSignatureValidation='no' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <MsiPackage EnableSignatureValidation=\"yes\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationYes()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Payload SuppressSignatureValidation='yes' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Payload />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        private static string UnformattedDocumentString(XDocument document)
        {
            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                document.Save(writer, SaveOptions.DisableFormatting);
            }

            return sb.ToString();
        }

        private class DummyMessaging : IMessaging
        {
            public bool EncounteredError { get; set; }

            public int LastErrorNumber { get; set; }

            public bool ShowVerboseMessages { get; set; }

            public bool SuppressAllWarnings { get; set; }

            public bool WarningsAsError { get; set; }

            public void ElevateWarningMessage(int warningNumber)
            {
            }

            public string FormatMessage(Message message) => String.Empty;

            public void SetListener(IMessageListener listener)
            {
            }

            public void SuppressWarningMessage(int warningNumber)
            {
            }

            public void Write(Message message)
            {
            }

            public void Write(string message, bool verbose = false)
            {
            }
        }
    }
}
