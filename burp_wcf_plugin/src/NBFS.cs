/*
 * WCF Binary Soap Decoder (MC-NBFS)
 * by Brian Holyfield, Gotham Digital Science (labs@gdssecurity.com)
 * 
 * Portions Copyright (c) 2009, Richard Berg, All Rights Reserved
 *  
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list 
 * of conditions and the following disclaimer. Redistributions in binary form must 
 * reproduce the above copyright notice, this list of conditions and the following 
 * disclaimer in the documentation and/or other materials provided with the distribution.
 * Neither the name of Richard Berg nor the names of its contributors may be used to 
 * endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS 
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY 
 * AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER 
 * IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

public class NBFSNetConsole
{
    public static void Main(string[] argv)
    {
        if (argv.Length == 2)
        {
            try
            {
                NBFSNet NBFS = new NBFSNet();

                if (argv[0].ToLower().Equals("encode"))
                {
                    Console.WriteLine(Convert.ToBase64String(NBFS.EncodeBinaryXML(System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(argv[1])))));
                }
                else
                {
                    Console.WriteLine(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(NBFS.DecodeBinaryXML(Convert.FromBase64String(argv[1])))));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(e.Message)));
            }
        }
        else
        {
            Console.WriteLine("Usage: NBFS [encode|decode] Base64Data\n\nNOTE: All output, including exceptions, will be returned as a Base64 string.");
        }
    }
}

public class NBFSNet
{
    private WcfBinaryCodec m_wcfBinaryCodec = new WcfBinaryCodec(Encoding.UTF8);

    public NBFSNet() { }

    public string DecodeBinaryXML(byte[] encodedXML)
    {
        if (encodedXML == null)
        {
            return "";
        }
        return m_wcfBinaryCodec.DecodeBinaryXML(encodedXML, false);
    }

    public byte[] EncodeBinaryXML(string xml)
    {
        if (String.IsNullOrEmpty(xml.Trim()))
        {
            return null;
        }
        return m_wcfBinaryCodec.EncodeBinaryXML(xml);
    }
}

public class WcfBinaryCodec
{
    public WcfBinaryCodec()
    { }

    public WcfBinaryCodec(Encoding encoding)
    {
        m_encoding = encoding;
    }

    Encoding m_encoding = Encoding.UTF8;

    /// <summary>
    /// Decode a bytestream that was encoded by WCF's BinaryEncodingBindingElement.  Will throw if the bytestream does
    /// not decode properly or the result is not valid XML.  I/O streams are flushed but not closed.
    /// </summary>        
    /// <param name="explodeNewlines">if true, the returned string will be nicely indented according to 
    /// element depth, and each attribute will be placed on its own line</param>
    /// <returns></returns>
    public void DecodeBinaryXML(Stream binaryInput, Stream xmlOutput, bool? explodeNewlines)
    {
        // defaults
        var explode = explodeNewlines ?? false;

        // parse bytestream into the XML DOM
        var doc = new XmlDocument();
        using (var binaryReader = XmlDictionaryReader.CreateBinaryReader(binaryInput, WcfDictionaryBuilder.Dict, XmlDictionaryReaderQuotas.Max))
        {
            doc.Load(binaryReader);
        }

        // write document to the output stream with customized settings
        var settings = new XmlWriterSettings()
        {
            CheckCharacters = false,
            CloseOutput = false,
            ConformanceLevel = ConformanceLevel.Auto,
            Encoding = m_encoding,
            Indent = explode,
            IndentChars = "\t",
            NewLineChars = Environment.NewLine,
            NewLineHandling = explode ? NewLineHandling.Replace : NewLineHandling.None,
            NewLineOnAttributes = explode
        };
        using (var writer = XmlWriter.Create(xmlOutput, settings))
        {
            doc.Save(writer);
            writer.Flush();
            xmlOutput.Flush();
        }
    }

    public string DecodeBinaryXML(byte[] binaryInput, bool? explodeNewLines)
    {
        var input = new MemoryStream(binaryInput);
        var output = new MemoryStream();
        DecodeBinaryXML(input, output, explodeNewLines);
        output.Seek(0, SeekOrigin.Begin);
        return new StreamReader(output, m_encoding).ReadToEnd();
    }

    /// <summary>
    /// Encode a text stream into a binary XML stream compatible with WCF's BinaryEncodingBindingElement.  Will throw if 
    /// the input stream cannot be parsed into an XML document.  I/O streams are flushed but not closed.
    /// </summary>
    /// <param name="xmlInput"></param>
    /// <param name="binaryOutput"></param>
    public void EncodeBinaryXML(Stream xmlInput, Stream binaryOutput)
    {
        // parse string into the XML DOM
        var doc = new XmlDocument();
        doc.Load(xmlInput);

        // write bytestream
        using (var binaryWriter = XmlDictionaryWriter.CreateBinaryWriter(binaryOutput, WcfDictionaryBuilder.Dict, null, false))
        {
            doc.Save(binaryWriter);
            binaryWriter.Flush();
            binaryOutput.Flush();
        }
    }

    public byte[] EncodeBinaryXML(string xmlInput)
    {
        var input = new MemoryStream(m_encoding.GetBytes(xmlInput));
        var output = new MemoryStream();
        EncodeBinaryXML(input, output);
        return output.ToArray();
    }
}

public static class WcfDictionaryBuilder
{
    private static XmlDictionary dict;

    public static XmlDictionary Dict
    {
        get { return dict; }
    }

    static WcfDictionaryBuilder()
    {
        dict = new XmlDictionary();
        dict.Add("mustUnderstand");
        dict.Add("Envelope");
        dict.Add("http://www.w3.org/2003/05/soap-envelope");
        dict.Add("http://www.w3.org/2005/08/addressing");
        dict.Add("Header");
        dict.Add("Action");
        dict.Add("To");
        dict.Add("Body");
        dict.Add("Algorithm");
        dict.Add("RelatesTo");
        dict.Add("http://www.w3.org/2005/08/addressing/anonymous");
        dict.Add("URI");
        dict.Add("Reference");
        dict.Add("MessageID");
        dict.Add("Id");
        dict.Add("Identifier");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/rm");
        dict.Add("Transforms");
        dict.Add("Transform");
        dict.Add("DigestMethod");
        dict.Add("DigestValue");
        dict.Add("Address");
        dict.Add("ReplyTo");
        dict.Add("SequenceAcknowledgement");
        dict.Add("AcknowledgementRange");
        dict.Add("Upper");
        dict.Add("Lower");
        dict.Add("BufferRemaining");
        dict.Add("http://schemas.microsoft.com/ws/2006/05/rm");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/rm/SequenceAcknowledgement");
        dict.Add("SecurityTokenReference");
        dict.Add("Sequence");
        dict.Add("MessageNumber");
        dict.Add("http://www.w3.org/2000/09/xmldsig#");
        dict.Add("http://www.w3.org/2000/09/xmldsig#enveloped-signature");
        dict.Add("KeyInfo");
        dict.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
        dict.Add("http://www.w3.org/2001/04/xmlenc#");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/sc");
        dict.Add("DerivedKeyToken");
        dict.Add("Nonce");
        dict.Add("Signature");
        dict.Add("SignedInfo");
        dict.Add("CanonicalizationMethod");
        dict.Add("SignatureMethod");
        dict.Add("SignatureValue");
        dict.Add("DataReference");
        dict.Add("EncryptedData");
        dict.Add("EncryptionMethod");
        dict.Add("CipherData");
        dict.Add("CipherValue");
        dict.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
        dict.Add("Security");
        dict.Add("Timestamp");
        dict.Add("Created");
        dict.Add("Expires");
        dict.Add("Length");
        dict.Add("ReferenceList");
        dict.Add("ValueType");
        dict.Add("Type");
        dict.Add("EncryptedHeader");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd");
        dict.Add("RequestSecurityTokenResponseCollection");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust#BinarySecret");
        dict.Add("http://schemas.microsoft.com/ws/2006/02/transactions");
        dict.Add("s");
        dict.Add("Fault");
        dict.Add("MustUnderstand");
        dict.Add("role");
        dict.Add("relay");
        dict.Add("Code");
        dict.Add("Reason");
        dict.Add("Text");
        dict.Add("Node");
        dict.Add("Role");
        dict.Add("Detail");
        dict.Add("Value");
        dict.Add("Subcode");
        dict.Add("NotUnderstood");
        dict.Add("qname");
        dict.Add("");
        dict.Add("From");
        dict.Add("FaultTo");
        dict.Add("EndpointReference");
        dict.Add("PortType");
        dict.Add("ServiceName");
        dict.Add("PortName");
        dict.Add("ReferenceProperties");
        dict.Add("RelationshipType");
        dict.Add("Reply");
        dict.Add("a");
        dict.Add("http://schemas.xmlsoap.org/ws/2006/02/addressingidentity");
        dict.Add("Identity");
        dict.Add("Spn");
        dict.Add("Upn");
        dict.Add("Rsa");
        dict.Add("Dns");
        dict.Add("X509v3Certificate");
        dict.Add("http://www.w3.org/2005/08/addressing/fault");
        dict.Add("ReferenceParameters");
        dict.Add("IsReferenceParameter");
        dict.Add("http://www.w3.org/2005/08/addressing/reply");
        dict.Add("http://www.w3.org/2005/08/addressing/none");
        dict.Add("Metadata");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/08/addressing");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/08/addressing/fault");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/06/addressingex");
        dict.Add("RedirectTo");
        dict.Add("Via");
        dict.Add("http://www.w3.org/2001/10/xml-exc-c14n#");
        dict.Add("PrefixList");
        dict.Add("InclusiveNamespaces");
        dict.Add("ec");
        dict.Add("SecurityContextToken");
        dict.Add("Generation");
        dict.Add("Label");
        dict.Add("Offset");
        dict.Add("Properties");
        dict.Add("Cookie");
        dict.Add("wsc");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/sc");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/sc/dk");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/sc/sct");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/RST/SCT");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/RSTR/SCT");
        dict.Add("RenewNeeded");
        dict.Add("BadContextToken");
        dict.Add("c");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/sc/dk");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/sc/sct");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RST/SCT");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/SCT");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RST/SCT/Renew");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/SCT/Renew");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RST/SCT/Cancel");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/SCT/Cancel");
        dict.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc");
        dict.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128");
        dict.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc");
        dict.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192");
        dict.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc");
        dict.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256");
        dict.Add("http://www.w3.org/2001/04/xmlenc#des-cbc");
        dict.Add("http://www.w3.org/2000/09/xmldsig#dsa-sha1");
        dict.Add("http://www.w3.org/2001/10/xml-exc-c14n#WithComments");
        dict.Add("http://www.w3.org/2000/09/xmldsig#hmac-sha1");
        dict.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/sc/dk/p_sha1");
        dict.Add("http://www.w3.org/2001/04/xmlenc#ripemd160");
        dict.Add("http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p");
        dict.Add("http://www.w3.org/2000/09/xmldsig#rsa-sha1");
        dict.Add("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        dict.Add("http://www.w3.org/2001/04/xmlenc#rsa-1_5");
        dict.Add("http://www.w3.org/2000/09/xmldsig#sha1");
        dict.Add("http://www.w3.org/2001/04/xmlenc#sha256");
        dict.Add("http://www.w3.org/2001/04/xmlenc#sha512");
        dict.Add("http://www.w3.org/2001/04/xmlenc#tripledes-cbc");
        dict.Add("http://www.w3.org/2001/04/xmlenc#kw-tripledes");
        dict.Add("http://schemas.xmlsoap.org/2005/02/trust/tlsnego#TLS_Wrap");
        dict.Add("http://schemas.xmlsoap.org/2005/02/trust/spnego#GSS_Wrap");
        dict.Add("http://schemas.microsoft.com/ws/2006/05/security");
        dict.Add("dnse");
        dict.Add("o");
        dict.Add("Password");
        dict.Add("PasswordText");
        dict.Add("Username");
        dict.Add("UsernameToken");
        dict.Add("BinarySecurityToken");
        dict.Add("EncodingType");
        dict.Add("KeyIdentifier");
        dict.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
        dict.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#HexBinary");
        dict.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Text");
        dict.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509SubjectKeyIdentifier");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-kerberos-token-profile-1.1#GSS_Kerberosv5_AP_REQ");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-kerberos-token-profile-1.1#GSS_Kerberosv5_AP_REQ1510");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.0#SAMLAssertionID");
        dict.Add("Assertion");
        dict.Add("urn:oasis:names:tc:SAML:1.0:assertion");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-rel-token-profile-1.0.pdf#license");
        dict.Add("FailedAuthentication");
        dict.Add("InvalidSecurityToken");
        dict.Add("InvalidSecurity");
        dict.Add("k");
        dict.Add("SignatureConfirmation");
        dict.Add("TokenType");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#ThumbprintSHA1");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKeySHA1");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLID");
        dict.Add("AUTH-HASH");
        dict.Add("RequestSecurityTokenResponse");
        dict.Add("KeySize");
        dict.Add("RequestedTokenReference");
        dict.Add("AppliesTo");
        dict.Add("Authenticator");
        dict.Add("CombinedHash");
        dict.Add("BinaryExchange");
        dict.Add("Lifetime");
        dict.Add("RequestedSecurityToken");
        dict.Add("Entropy");
        dict.Add("RequestedProofToken");
        dict.Add("ComputedKey");
        dict.Add("RequestSecurityToken");
        dict.Add("RequestType");
        dict.Add("Context");
        dict.Add("BinarySecret");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/spnego");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/tlsnego");
        dict.Add("wst");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/trust");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/RST/Issue");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/RSTR/Issue");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/Issue");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/CK/PSHA1");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/SymmetricKey");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/security/trust/Nonce");
        dict.Add("KeyType");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/trust/SymmetricKey");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/04/trust/PublicKey");
        dict.Add("Claims");
        dict.Add("InvalidRequest");
        dict.Add("RequestFailed");
        dict.Add("SignWith");
        dict.Add("EncryptWith");
        dict.Add("EncryptionAlgorithm");
        dict.Add("CanonicalizationAlgorithm");
        dict.Add("ComputedKeyAlgorithm");
        dict.Add("UseKey");
        dict.Add("http://schemas.microsoft.com/net/2004/07/secext/WS-SPNego");
        dict.Add("http://schemas.microsoft.com/net/2004/07/secext/TLSNego");
        dict.Add("t");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/Issue");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/Issue");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/SymmetricKey");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/CK/PSHA1");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/Nonce");
        dict.Add("RenewTarget");
        dict.Add("CancelTarget");
        dict.Add("RequestedTokenCancelled");
        dict.Add("RequestedAttachedReference");
        dict.Add("RequestedUnattachedReference");
        dict.Add("IssuedTokens");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/Renew");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/Cancel");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/trust/PublicKey");
        dict.Add("Access");
        dict.Add("AccessDecision");
        dict.Add("Advice");
        dict.Add("AssertionID");
        dict.Add("AssertionIDReference");
        dict.Add("Attribute");
        dict.Add("AttributeName");
        dict.Add("AttributeNamespace");
        dict.Add("AttributeStatement");
        dict.Add("AttributeValue");
        dict.Add("Audience");
        dict.Add("AudienceRestrictionCondition");
        dict.Add("AuthenticationInstant");
        dict.Add("AuthenticationMethod");
        dict.Add("AuthenticationStatement");
        dict.Add("AuthorityBinding");
        dict.Add("AuthorityKind");
        dict.Add("AuthorizationDecisionStatement");
        dict.Add("Binding");
        dict.Add("Condition");
        dict.Add("Conditions");
        dict.Add("Decision");
        dict.Add("DoNotCacheCondition");
        dict.Add("Evidence");
        dict.Add("IssueInstant");
        dict.Add("Issuer");
        dict.Add("Location");
        dict.Add("MajorVersion");
        dict.Add("MinorVersion");
        dict.Add("NameIdentifier");
        dict.Add("Format");
        dict.Add("NameQualifier");
        dict.Add("Namespace");
        dict.Add("NotBefore");
        dict.Add("NotOnOrAfter");
        dict.Add("saml");
        dict.Add("Statement");
        dict.Add("Subject");
        dict.Add("SubjectConfirmation");
        dict.Add("SubjectConfirmationData");
        dict.Add("ConfirmationMethod");
        dict.Add("urn:oasis:names:tc:SAML:1.0:cm:holder-of-key");
        dict.Add("urn:oasis:names:tc:SAML:1.0:cm:sender-vouches");
        dict.Add("SubjectLocality");
        dict.Add("DNSAddress");
        dict.Add("IPAddress");
        dict.Add("SubjectStatement");
        dict.Add("urn:oasis:names:tc:SAML:1.0:am:unspecified");
        dict.Add("xmlns");
        dict.Add("Resource");
        dict.Add("UserName");
        dict.Add("urn:oasis:names:tc:SAML:1.1:nameid-format:WindowsDomainQualifiedName");
        dict.Add("EmailName");
        dict.Add("urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress");
        dict.Add("u");
        dict.Add("ChannelInstance");
        dict.Add("http://schemas.microsoft.com/ws/2005/02/duplex");
        dict.Add("Encoding");
        dict.Add("MimeType");
        dict.Add("CarriedKeyName");
        dict.Add("Recipient");
        dict.Add("EncryptedKey");
        dict.Add("KeyReference");
        dict.Add("e");
        dict.Add("http://www.w3.org/2001/04/xmlenc#Element");
        dict.Add("http://www.w3.org/2001/04/xmlenc#Content");
        dict.Add("KeyName");
        dict.Add("MgmtData");
        dict.Add("KeyValue");
        dict.Add("RSAKeyValue");
        dict.Add("Modulus");
        dict.Add("Exponent");
        dict.Add("X509Data");
        dict.Add("X509IssuerSerial");
        dict.Add("X509IssuerName");
        dict.Add("X509SerialNumber");
        dict.Add("X509Certificate");
        dict.Add("AckRequested");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/rm/AckRequested");
        dict.Add("AcksTo");
        dict.Add("Accept");
        dict.Add("CreateSequence");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/rm/CreateSequence");
        dict.Add("CreateSequenceRefused");
        dict.Add("CreateSequenceResponse");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/rm/CreateSequenceResponse");
        dict.Add("FaultCode");
        dict.Add("InvalidAcknowledgement");
        dict.Add("LastMessage");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/rm/LastMessage");
        dict.Add("LastMessageNumberExceeded");
        dict.Add("MessageNumberRollover");
        dict.Add("Nack");
        dict.Add("netrm");
        dict.Add("Offer");
        dict.Add("r");
        dict.Add("SequenceFault");
        dict.Add("SequenceTerminated");
        dict.Add("TerminateSequence");
        dict.Add("http://schemas.xmlsoap.org/ws/2005/02/rm/TerminateSequence");
        dict.Add("UnknownSequence");
        dict.Add("http://schemas.microsoft.com/ws/2006/02/tx/oletx");
        dict.Add("oletx");
        dict.Add("OleTxTransaction");
        dict.Add("PropagationToken");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wscoor");
        dict.Add("wscoor");
        dict.Add("CreateCoordinationContext");
        dict.Add("CreateCoordinationContextResponse");
        dict.Add("CoordinationContext");
        dict.Add("CurrentContext");
        dict.Add("CoordinationType");
        dict.Add("RegistrationService");
        dict.Add("Register");
        dict.Add("RegisterResponse");
        dict.Add("ProtocolIdentifier");
        dict.Add("CoordinatorProtocolService");
        dict.Add("ParticipantProtocolService");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wscoor/CreateCoordinationContext");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wscoor/CreateCoordinationContextResponse");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wscoor/Register");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wscoor/RegisterResponse");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wscoor/fault");
        dict.Add("ActivationCoordinatorPortType");
        dict.Add("RegistrationCoordinatorPortType");
        dict.Add("InvalidState");
        dict.Add("InvalidProtocol");
        dict.Add("InvalidParameters");
        dict.Add("NoActivity");
        dict.Add("ContextRefused");
        dict.Add("AlreadyRegistered");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat");
        dict.Add("wsat");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Completion");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Durable2PC");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Volatile2PC");
        dict.Add("Prepare");
        dict.Add("Prepared");
        dict.Add("ReadOnly");
        dict.Add("Commit");
        dict.Add("Rollback");
        dict.Add("Committed");
        dict.Add("Aborted");
        dict.Add("Replay");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Commit");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Rollback");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Committed");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Aborted");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Prepare");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Prepared");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/ReadOnly");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/Replay");
        dict.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat/fault");
        dict.Add("CompletionCoordinatorPortType");
        dict.Add("CompletionParticipantPortType");
        dict.Add("CoordinatorPortType");
        dict.Add("ParticipantPortType");
        dict.Add("InconsistentInternalState");
        dict.Add("mstx");
        dict.Add("Enlistment");
        dict.Add("protocol");
        dict.Add("LocalTransactionId");
        dict.Add("IsolationLevel");
        dict.Add("IsolationFlags");
        dict.Add("Description");
        dict.Add("Loopback");
        dict.Add("RegisterInfo");
        dict.Add("ContextId");
        dict.Add("TokenId");
        dict.Add("AccessDenied");
        dict.Add("InvalidPolicy");
        dict.Add("CoordinatorRegistrationFailed");
        dict.Add("TooManyEnlistments");
        dict.Add("Disabled");
        dict.Add("ActivityId");
        dict.Add("http://schemas.microsoft.com/2004/09/ServiceModel/Diagnostics");
        dict.Add("http://docs.oasis-open.org/wss/oasis-wss-kerberos-token-profile-1.1#Kerberosv5APREQSHA1");
        dict.Add("http://schemas.xmlsoap.org/ws/2002/12/policy");
        dict.Add("FloodMessage");
        dict.Add("LinkUtility");
        dict.Add("Hops");
        dict.Add("http://schemas.microsoft.com/net/2006/05/peer/HopCount");
        dict.Add("PeerVia");
        dict.Add("http://schemas.microsoft.com/net/2006/05/peer");
        dict.Add("PeerFlooder");
        dict.Add("PeerTo");
        dict.Add("http://schemas.microsoft.com/ws/2005/05/routing");
        dict.Add("PacketRoutable");
        dict.Add("http://schemas.microsoft.com/ws/2005/05/addressing/none");
        dict.Add("http://schemas.microsoft.com/ws/2005/05/envelope/none");
        dict.Add("http://www.w3.org/2001/XMLSchema-instance");
        dict.Add("http://www.w3.org/2001/XMLSchema");
        dict.Add("nil");
        dict.Add("type");
        dict.Add("char");
        dict.Add("boolean");
        dict.Add("byte");
        dict.Add("unsignedByte");
        dict.Add("short");
        dict.Add("unsignedShort");
        dict.Add("int");
        dict.Add("unsignedInt");
        dict.Add("long");
        dict.Add("unsignedLong");
        dict.Add("float");
        dict.Add("double");
        dict.Add("decimal");
        dict.Add("dateTime");
        dict.Add("string");
        dict.Add("base64Binary");
        dict.Add("anyType");
        dict.Add("duration");
        dict.Add("guid");
        dict.Add("anyURI");
        dict.Add("QName");
        dict.Add("time");
        dict.Add("date");
        dict.Add("hexBinary");
        dict.Add("gYearMonth");
        dict.Add("gYear");
        dict.Add("gMonthDay");
        dict.Add("gDay");
        dict.Add("gMonth");
        dict.Add("integer");
        dict.Add("positiveInteger");
        dict.Add("negativeInteger");
        dict.Add("nonPositiveInteger");
        dict.Add("nonNegativeInteger");
        dict.Add("normalizedString");
        dict.Add("ConnectionLimitReached");
        dict.Add("http://schemas.xmlsoap.org/soap/envelope/");
        dict.Add("Actor");
        dict.Add("Faultcode");
        dict.Add("Faultstring");
        dict.Add("Faultactor");
        dict.Add("Detail");
    }
}

