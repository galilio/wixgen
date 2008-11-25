//-------------------------------------------------------------------------------------------------
// <summary>
// The WixGen codegen (.wxs) tool application.
// </summary>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Sitronics.Installer
{
	//    using Microsoft.Tools.WindowsInstallerXml;

	internal enum WixVersion
	{
		Wix2,
		Wix3
	}

	/// <summary>
	/// The main entry point for tallow.
	/// </summary>
	public static class WixGen
	{
		private const string LegalShortFilenameCharacters = @"[^\\\?|><:/\*""\+,;=\[\]\. ]";
		                     // illegal: \ ? | > < : / * " + , ; = [ ] . (space)

		private static Regex LegalShortFilename =
			new Regex(String.Concat("^", LegalShortFilenameCharacters, @"{1,8}(\.", LegalShortFilenameCharacters, "{0,3})?$"),
			          RegexOptions.Compiled);

		/// <summary>
		/// The main entry point for candle.
		/// </summary>
		/// <param name="args">Commandline arguments for the application.</param>
		/// <returns>Returns the application error code.</returns>
		[MTAThread]
		public static int Main(string[] args)
		{
			try
			{
				/*WixGenMain wixgen = */new WixGenMain(args);
			}
/*
            catch (WixException we)
            {
                string errorFileName = "WixGen.exe";
                Console.Error.WriteLine("\r\n\r\n{0} : fatal error WGEN{1:0000}: {2}", errorFileName, (int)we.Type, we.Message);
                Console.Error.WriteLine();

                return 1;
            }
 */
			catch (WixGenException e)
			{
				Console.Error.WriteLine("\r\n\r\nWixGen.exe : fatal error WGEN0002: {0}\r\nStack Trace:\r\n{1}", e.Message, e.StackTrace);
				return 2;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("\r\n\r\nWixGen.exe : fatal error WGEN0001: {0}\r\n\r\nStack Trace:\r\n{1}", e.Message,
				                        e.StackTrace);
				return 1;
			}

			return 0;
		}

		#region Nested type: ComponentGroup

		internal class ComponentGroup
		{
			public StringCollection m_components;
			public string m_name;

			public ComponentGroup(string name)
			{
				m_name = name;
				m_components = new StringCollection();
			}
		}

		#endregion

		#region Nested type: FileFilter

		internal struct FileFilter
		{
			public bool FailOnEmpty;
			public string Mask;

			public FileFilter(string mask)
			{
				Mask = mask;
				FailOnEmpty = false;
			}

			public FileFilter(string mask, bool failOnEmpty)
			{
				Mask = mask;
				FailOnEmpty = failOnEmpty;
			}
		} ;

		#endregion

		#region Nested type: WixGenMain

		/// <summary>
		/// Main class for tallow.
		/// </summary>
		internal class WixGenMain
		{
//			private const int MaxPath = 255;

/*
			private static readonly UIntPtr HkeyClassesRoot = (UIntPtr)0x80000000;
            private static readonly UIntPtr HkeyCurrentUser = (UIntPtr)0x80000001;
            private static readonly UIntPtr HkeyLocalMachine = (UIntPtr)0x80000002;
            private static readonly UIntPtr HkeyUsers = (UIntPtr)0x80000003;

            private const uint Delete = 0x00010000;
            private const uint ReadOnly = 0x00020000;
            private const uint WriteDac = 0x00040000;
            private const uint WriteOwner = 0x00080000;
            private const uint Synchronize = 0x00100000;
            private const uint StandardRightsRequired = 0x000F0000;
            private const uint StandardRightsAll = 0x001F0000;

            private const uint GenericRead = 0x80000000;
            private const uint GenericWrite = 0x40000000;
            private const uint GenericExecute = 0x20000000;
            private const uint GenericAll = 0x10000000;
*/
			private const string VAR_BASEDIR = "_BaseDir";

			private ArrayList m_componentGroups;
			private bool m_defaultExcludes;
			private XmlDocument m_doc;
			private int m_fileCounter;
			private string m_namePrefix;

			/// <summary>
			/// namespace Root xmlElements
			/// </summary>
			private string m_namespaceURI;

			private string m_outputDirectory;

//            private int m_directoryCounter;

			// Property list from command line -D parameters
			private StringDictionary m_properties;
			private bool m_showHelp;
			private bool m_showLogo;
			private StringCollection m_templateFiles;

			/// <summary>
			/// output format to WiX 3.0
			///  </summary>
			private WixVersion m_wixVersion;

//            private int componentCount;
/*
            private const UInt32 LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

            [DllImport ("kernel32.dll", CallingConvention=CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr LoadLibraryEx(string DllPath, IntPtr File, UInt32 Flags);
*/

			/// <summary>
			/// Main method for the tallow application within the WixGenMain class.
			/// </summary>
			/// <param name="args">Commandline arguments to the application.</param>
			public WixGenMain(string[] args)
			{
				m_defaultExcludes = true;
				m_showLogo = true;
				m_wixVersion = WixVersion.Wix2;
				m_templateFiles = new StringCollection();
				m_componentGroups = new ArrayList();
				m_properties = new StringDictionary();


				// parse the command line
				ParseCommandLine(args);

				// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
				if (m_properties.Count > 0)
				{
					// Displays the values in the array.
					Console.WriteLine("Defined properties:");
					foreach (String iKey in m_properties.Keys)
						Console.WriteLine("  '{0}' = '{1}'", iKey, m_properties[iKey]);
					Console.WriteLine();
				}

				if (m_templateFiles.Count == 0 && !m_showHelp)
				{
					GenError("No specified template");
				}

				// get the assemblies
				Assembly wixgenAssembly = Assembly.GetExecutingAssembly();

				if (m_showLogo)
				{
					Console.WriteLine("WiX file generate utility {0}", wixgenAssembly.GetName().Version);
					Console.WriteLine("Copyright (C) SITRONICS Telecom Solutions 2006-2007. All rights reserved.");
					if (m_wixVersion == WixVersion.Wix3)
					{
						Console.WriteLine("Output format for WiX 3.0.");
					}
					Console.WriteLine();
				}

				if (m_showHelp)
				{
					ShowHelp();
					return; // exit
				}

				if (m_templateFiles.Count > 0)
				{
					foreach (string templateFile in m_templateFiles)
					{
						ProcessTemplate(templateFile);
					}
				}
			}

			public static void ShowHelp()
			{
				Console.WriteLine(" usage:  WixGen.exe [-?] [-nologo] [-wix3] [-outdir directory] [-t templateFile [-t templateFile] ...]");
				Console.WriteLine();
				Console.WriteLine("   -wix3               Output in WiX 3 format");
				Console.WriteLine("   -D:<name>=<value>   Define value for given property. Use in code as ${name}.");
				Console.WriteLine("   -t                  Template file name with extension .wix-t");
				Console.WriteLine("   -noexcludes         Disable default excludes (vssver.scc)");
				Console.WriteLine("   -outdir             Output directory for .wix files");
				Console.WriteLine("   -nologo             Skip printing tallow logo information");
				Console.WriteLine("   -?                  This help information");
			}

			/// <summary>
			/// Parse the commandline arguments.
			/// </summary>
			/// <param name="args">Commandline arguments.</param>
			private void ParseCommandLine(string[] args)
			{
				for (int i = 0; i < args.Length; ++i)
				{
					string arg = args[i];
					if (String.IsNullOrEmpty(arg)) // skip blank arguments
					{
						continue;
					}

					if ('-' == arg[0] || '/' == arg[0])
					{
						if (arg.Length < 2)
							throw new ArgumentException("Invalid command line", arg);

						string parameter = arg.Substring(1);

						if (parameter[0] == 'D') // -D:<name>=<value>
						{
							if (parameter.Length <= 1 || parameter[1] != ':')
								throw new ArgumentException("Symbol ':' expected at -D parameter");

							parameter = parameter.Substring(2);

							string[] value = parameter.Split("=".ToCharArray(), 2);

							if (value.Length < 1 || value[0].Length <= 0)
							{
								throw new ArgumentException("must specify a property name with -D");
							}

							if (1 == value.Length)
							{
								m_properties.Add(value[0], "");
							}
							else
							{
								m_properties.Add(value[0].Trim(), value[1].Trim());
							}
						}
						else if ("t" == parameter || "template" == parameter) // -t <fileName.wxs_t>
						{
							if (args.Length <= ++i || args[i].Length == 0  || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a template file for parameter -t");
							}

							m_templateFiles.Add(args[i]);
						}
						else if ("outdir" == parameter)
						{
							if (args.Length <= ++i || args[i].Length == 0 || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a directory for parameter -outdir");
							}

							m_outputDirectory = args[i];
						}
						else if ("nologo" == parameter)
						{
							m_showLogo = false;
						}
						else if ("noexcludes" == parameter)
						{
							m_defaultExcludes = false;
						}
						else if ("?" == parameter || "help" == parameter)
						{
							m_showHelp = true;
						}
						else if ("wix3" == parameter)
						{
							m_wixVersion = WixVersion.Wix3;
						}
						else
						{
							throw new ArgumentException("unknown parameter", String.Concat("-", parameter));
						}
					}
					else if ('@' == arg[0])
					{
						using (StreamReader reader = new StreamReader(arg.Substring(1)))
						{
							string line;
							ArrayList newArgs = new ArrayList();

							while (null != (line = reader.ReadLine()))
							{
								string newArg = "";
								bool betweenQuotes = false;
								for (int j = 0; j < line.Length; ++j)
								{
									// skip whitespace
									if (!betweenQuotes && (' ' == line[j] || '\t' == line[j]))
									{
										if (!String.IsNullOrEmpty(newArg))
										{
											newArgs.Add(newArg);
											newArg = null;
										}

										continue;
									}

									// if we're escaping a quote
									if ('\\' == line[j] && j < line.Length - 1 && '"' == line[j + 1])
									{
										++j;
									}
									else if ('"' == line[j]) // if we've hit a new quote
									{
										betweenQuotes = !betweenQuotes;
										continue;
									}

									newArg = String.Concat(newArg, line[j]);
								}
								if (!String.IsNullOrEmpty(newArg))
								{
									newArgs.Add(newArg);
								}
							}
							string[] ar = (string[])newArgs.ToArray(typeof(string));
							ParseCommandLine(ar);
						}
					}
					else
					{
						throw new ArgumentException(String.Concat("unexpected argument on command line: ", arg));
					}
				}
			}

			private static void GenError(string format, params object[] args)
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat(format, args);
				throw new XmlException(sb.ToString());
			}
/*
			private static void GenWarning(string format, params object[] args)
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat(format, args);
				Console.WriteLine("WixGen.exe: warning {0}", sb.ToString());
			}
*/
			private static XmlElement getFirstChildElement(XmlElement parent)
			{
				XmlElement result = null;
				foreach (XmlNode child in parent.ChildNodes)
				{
					result = child as XmlElement;
					if (result != null)
						break;
				}

				return result;
			}

			private static string _GetAttribute(XmlElement node, string attributeName)
			{
				if (!node.HasAttribute(attributeName))
					GenError("Absent attribute '{0}' at node '{1}'", attributeName, node.Name);

				return node.GetAttribute(attributeName);
			}

			private string GetAttributeDecoded(XmlElement node, string attributeName)
			{
				return DecodeValue(_GetAttribute(node, attributeName));
			}

			private static void CopyAttribute(XmlElement dest, XmlElement src, string attributeName)
			{
				dest.SetAttribute(attributeName, _GetAttribute(src, attributeName));
			}

			// Заменяет в строке ссылки на параметры значениями параметров
			// ${Param1} -> ParamValue
			private string DecodeValue(string Value)
			{
				string result = Value;
				while (true)
				{
					int iLeft = result.IndexOf("${");
					if (iLeft < 0)
						break;

					int iRight = result.IndexOf("}", iLeft);
					if (iRight < 0)
						return result; // WARNING: Invalid property format

					string propName = result.Substring(iLeft + 2, (iRight - 1) - (iLeft + 2) + 1);
					if (!m_properties.ContainsKey(propName))
					{
						throw new WixGenException(String.Format("Property '${{{0}}} is not defined' ", propName));
					}
					else
					{
						string propValue = m_properties[propName];
						result = result.Remove(iLeft, iRight - iLeft + 1);
						result = result.Insert(iLeft, propValue);
					}
				}

				return result;
			}

			/// <summary>
			/// Verifies if a filename is a valid short filename.
			/// </summary>
			/// <param name="filename">Filename to verify.</param>
			/// <returns>True if the filename is a valid short filename</returns>
			public static bool IsValidShortFilename(string filename)
			{
				if (null == filename || 0 == filename.Length)
				{
					return false;
				}

				return LegalShortFilename.IsMatch(filename);
			}

			/// <summary>
			/// Generates a short file/directory name using an identifier and long file/directory name as input.
			/// </summary>
			/// <param name="longName">The long file/directory name.</param>
			/// <param name="keepExtension">The option to keep the extension on generated short names.</param>
			/// <param name="args">Any additional information to include in the hash for the generated short name.</param>
			/// <returns>The generated 8.3-compliant short file/directory name.</returns>
			private static string GenerateShortName(string longName, bool keepExtension, params string[] args)
			{
				// collect all the data
				ArrayList strings = new ArrayList();
				strings.Add(longName);
				strings.AddRange(args);

				// prepare for hashing
				string stringData = String.Join("|", (string[]) strings.ToArray(typeof (string)));
				byte[] data = Encoding.Unicode.GetBytes(stringData);

				// hash the data
				byte[] hash;
				using (MD5 md5 = new MD5CryptoServiceProvider())
				{
					hash = md5.ComputeHash(data);
				}

				// generate the short file/directory name without an extension
				StringBuilder shortName = new StringBuilder(Convert.ToBase64String(hash));
				shortName.Remove(8, shortName.Length - 8);
				shortName.Replace('/', '_');
				shortName.Replace('+', '-');

				if (keepExtension)
				{
					string extension = Path.GetExtension(longName);

					if (4 < extension.Length)
					{
						extension = extension.Substring(0, 4);
					}

					shortName.Append(extension);

					// check the generated short name to ensure its still legal (the extension may not be legal)
					if (!IsValidShortFilename(shortName.ToString()))
					{
						// remove the extension (by truncating the generated file name back to the generated characters)
						shortName.Length -= extension.Length;
					}
				}

				return shortName.ToString().ToLower(CultureInfo.InvariantCulture);
			}

			/// <summary>
			/// Формирует GUID на основе базового GUID'а и хэша ключевого слова (строки)
			/// </summary>
			/// <param name="baseGuid">Базовый GUID</param>
			/// <param name="keyString">Ключевая строка</param>
			/// <returns>Сформированный GUID</returns>
			private static Guid ComputeNewGuid(Guid baseGuid, string keyString)
			{
				MD5 md5 = MD5.Create();
				byte[] hashResult = md5.ComputeHash(new ASCIIEncoding().GetBytes(keyString));
				byte[] guidBytes = baseGuid.ToByteArray();

				for (int i = 1; i < 16; i += 2)
				{
					guidBytes[i] = hashResult[i];
				}

				return new Guid(guidBytes);
			}

			private void ProcessTemplate(string templateFile)
			{
				m_componentGroups.Clear();

				FileInfo templFileInfo = new FileInfo(templateFile);
				if (!templFileInfo.Exists)
					GenError("File {0} not found !", templateFile);

				if (templFileInfo.Extension != ".wxs_t")
					GenError("Invalid file extesion {0}. Must be .wxs_t", templateFile);

				string outFileName = "";
				if (!String.IsNullOrEmpty(m_outputDirectory))
				{
					outFileName = m_outputDirectory;
					if (outFileName[outFileName.Length - 1] != '\\')
						outFileName += '\\';
				}

				outFileName += templFileInfo.Name;
				outFileName =
					outFileName.Remove(outFileName.Length - templFileInfo.Extension.Length, templFileInfo.Extension.Length);
				outFileName += ".wxs";

				m_fileCounter = 0;
//                m_directoryCounter = 0;

//                FileStream file = new FileStream(templateFile, Open, Read); 
				Console.WriteLine("[WixGen] Processing file \"{0}\"", templateFile);

				//Create the XmlDocument.
				m_doc = new XmlDocument();

				m_doc.Load(templateFile);

				XmlElement nodeWix = m_doc.DocumentElement;
				if (nodeWix == null || nodeWix.Name != "Wix")
					GenError("Node Wix not found");

				m_namespaceURI = nodeWix.NamespaceURI;

				XmlElement nodeFragment = getFirstChildElement(nodeWix);
				if (nodeFragment == null || nodeFragment.Name != "Fragment")
					GenError("Node 'Fragment' was absent");

				m_namePrefix = GetAttributeDecoded(nodeFragment, "Prefix");
				if (String.IsNullOrEmpty(m_namePrefix) || m_namePrefix.Length > 4)
					GenError("Invalid 'Fragment->Prefix' attribute value");

				nodeFragment.RemoveAttribute("Prefix");


				for (XmlNode child = nodeFragment.FirstChild; child != null;)
				{
					bool removeCurrentNode = false;

					XmlElement node = child as XmlElement;
					if (node != null)
					{
						if (node.Name == "TDirectoryRef")
						{
							XmlElement newNode = Process_DirectoryRef(node);
							node.ParentNode.InsertBefore(newNode, node);
							removeCurrentNode = true;
						}
					}

					if (removeCurrentNode)
					{
						XmlNode old = child;
						child = child.NextSibling;
						old.ParentNode.RemoveChild(old);
					}
					else
						child = child.NextSibling;
				}

				// Adding componet groups (element ComponentGroup)
				foreach (ComponentGroup compGroup in m_componentGroups)
				{
					XmlElement nodeComponentGroup = m_doc.CreateElement("ComponentGroup", m_namespaceURI);
					nodeComponentGroup.SetAttribute("Id", compGroup.m_name);

					foreach (string compId in compGroup.m_components)
					{
						XmlElement nodeCompRef = m_doc.CreateElement("ComponentRef", m_namespaceURI);
						nodeCompRef.SetAttribute("Id", compId);
						nodeComponentGroup.AppendChild(nodeCompRef);
					}

					nodeFragment.AppendChild(nodeComponentGroup);
				}

				Console.WriteLine("[WixGen] Output to file \"{0}\"", outFileName);
				m_doc.Save(outFileName);

				m_doc = null;
			}

			private XmlElement Process_DirectoryRef(XmlElement nodeTemplate)
			{
				XmlElement nodeDirectoryRef = m_doc.CreateElement("DirectoryRef", m_namespaceURI);

				CopyAttribute(nodeDirectoryRef, nodeTemplate, "Id");
//                nodeDirectoryRef.SetAttributeNode(parent.GetAttributeNode("Id"));
//                nodeDirectoryRef.SetAttribute("LongName", parent.GetAttribute("Name"));

				ComponentGroup compGroup = new ComponentGroup(GetAttributeDecoded(nodeTemplate, "ComponentGroupId"));

				// List Directory nodes
				foreach (XmlNode child in nodeTemplate.ChildNodes)
				{
					XmlElement node = child as XmlElement;
					if (node == null)
						continue;

					if (node.Name == "TDirectory")
					{
						XmlElement nodeDirectory = Process_Directory(compGroup, node);
						nodeDirectoryRef.AppendChild(nodeDirectory);
					}
					else if (node.Name == "TDirectoryTree")
					{
						XmlElement nodeDirectoryTree = Process_DirectoryTree(compGroup, node);
						nodeDirectoryRef.AppendChild(nodeDirectoryTree);
					}
					else if (node.Name == "TIncludeComponent")
					{
						Process_IncludeComponent(compGroup, node);
					}
					else
						GenError("Invalid node name - " + node.Name);
				}

				m_componentGroups.Add(compGroup);

				return nodeDirectoryRef;
			}

			private void Process_IncludeComponent(ComponentGroup compGroup, XmlElement nodeTemplate)
			{
				string componentId = GetAttributeDecoded(nodeTemplate, "Id");
				compGroup.m_components.Add(componentId);
			}

			private void setDirectoryName(ref XmlElement nodeDirectory, string dirName)
			{
				if (dirName == "." || IsValidShortFilename(dirName) || m_wixVersion == WixVersion.Wix3)
					nodeDirectory.SetAttribute("Name", dirName);
				else
				{
					nodeDirectory.SetAttribute("Name", GenerateShortName(dirName, false));
					nodeDirectory.SetAttribute("LongName", dirName);
				}
/*
				string dirShortName;
				if (dirName.Length <= 8)
					dirShortName = dirName;
				else
				{
					dirShortName = m_namePrefix + '_' + m_directoryCounter.ToString("000");
					m_directoryCounter++;
				}

				nodeDirectory.SetAttribute("Name", dirShortName);
				if (dirName != dirShortName)
					nodeDirectory.SetAttribute("LongName", dirName);
*/
			}

			private static bool IsIdLetter(char ch)
			{
				return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_';
			}

			public static string NormalizeWixId(string value)
			{
				const int MAX_ID_LENGTH = 72;
				StringBuilder result = new StringBuilder(value.Length);
				StringBuilder posfix = new StringBuilder();

				for (int i = 0; i < value.Length; ++i)
				{
					char ch = value[i];
					if (ch == '/' || ch == '\\')
						result.Append('.');
					else if (IsIdLetter(ch) || (ch >= '0' && ch <= '9') || ch == '.')
					{
						result.Append(ch);
					}
					else
					{
						result.Append('_');
						if (ch != '-')
							posfix.Append(((int) ch).ToString("X2"));
					}
				}

				if (posfix.Length > 0)
				{
					result.Append("__");
					result.Append(posfix);
				}

				if (result.Length > MAX_ID_LENGTH)
					result.Remove(0, result.Length - MAX_ID_LENGTH);

				if (!IsIdLetter(result[0]))
				{
					result[0] = '_';
				}

				return result.ToString();
			}

			internal void AppendFilesAndDirectories(ComponentGroup compGroup, ref XmlElement parentDirectory,
			                                        ref XmlElement parentComponent, DirectoryInfo sourceDirInfo, Guid baseGuid,
			                                        string basePath)
			{
				FileInfo[] files = sourceDirInfo.GetFiles();

				if (files.Length <= 0)
				{
					XmlElement nodeFile = m_doc.CreateElement("CreateFolder", m_namespaceURI);
					parentComponent.AppendChild(nodeFile);
				}
				else
				{
					foreach (FileInfo file in files)
					{
						XmlElement nodeFile = m_doc.CreateElement("File", m_namespaceURI);

						string fileShortName = GenerateShortName(file.Name, true);

						nodeFile.SetAttribute("Id", NormalizeWixId(file.Name + '.' + fileShortName + '.' + baseGuid));
						if (m_wixVersion == WixVersion.Wix3)
						{
							nodeFile.SetAttribute("Name", file.Name);
						}
						else
						{
							nodeFile.SetAttribute("Name", fileShortName);
							nodeFile.SetAttribute("LongName", file.Name);
						}
						nodeFile.SetAttribute("Source", file.FullName.Replace(basePath, "$(var." + VAR_BASEDIR + ")"));
						nodeFile.SetAttribute("Vital", "yes");

						parentComponent.AppendChild(nodeFile);
					}
				}

				DirectoryInfo[] dirs = sourceDirInfo.GetDirectories();
				foreach (DirectoryInfo dir in dirs)
				{
					XmlElement nodeSubDir = m_doc.CreateElement("Directory", m_namespaceURI);
					setDirectoryName(ref nodeSubDir, dir.Name);

					string dirSubName = dir.FullName.Replace(basePath, "");
					if (dirSubName.Length == 0 || dirSubName[0] != '\\')
						dirSubName = '\\' + dirSubName;

					string posfix = '.' + baseGuid.ToString().Substring(0, 8).ToUpper();
					string directoryId = NormalizeWixId("Dir" + dirSubName + posfix);
					string componentId = NormalizeWixId("CM" + dirSubName + posfix);
					Guid directoryGuid = ComputeNewGuid(baseGuid, dirSubName);

					nodeSubDir.SetAttribute("Id", directoryId);

					XmlElement nodeSubComponent = m_doc.CreateElement("Component", m_namespaceURI);

					compGroup.m_components.Add(componentId);

					nodeSubComponent.SetAttribute("Id", componentId);
					nodeSubComponent.SetAttribute("DiskId", "1");
					nodeSubComponent.SetAttribute("Guid", directoryGuid.ToString());
					nodeSubComponent.SetAttribute("KeyPath", "yes");

					AppendFilesAndDirectories(compGroup, ref nodeSubDir, ref nodeSubComponent, dir, directoryGuid, basePath);

					nodeSubDir.AppendChild(nodeSubComponent);
					parentDirectory.AppendChild(nodeSubDir);
				}
			}

			private XmlElement Process_DirectoryTree(ComponentGroup compGroup, XmlElement nodeTemplate)
			{
				XmlElement nodeDirectory = m_doc.CreateElement("Directory", m_namespaceURI);
				CopyAttribute(nodeDirectory, nodeTemplate, "Id");

				string dirName = GetAttributeDecoded(nodeTemplate, "Name");
				setDirectoryName(ref nodeDirectory, dirName);

				string sourceDir = GetAttributeDecoded(nodeTemplate, "SourceDir");
				nodeDirectory.AppendChild(
					m_doc.CreateProcessingInstruction("define", String.Format("{0}=\"{1}\"", VAR_BASEDIR, sourceDir)));

				XmlElement nodeComponent = m_doc.CreateElement("Component", m_namespaceURI);
				nodeDirectory.AppendChild(nodeComponent);

				string ComponentId = GetAttributeDecoded(nodeTemplate, "ComponentId");
				nodeComponent.SetAttribute("Id", ComponentId);
				nodeComponent.SetAttribute("DiskId", "1");
				CopyAttribute(nodeComponent, nodeTemplate, "Guid");

				Guid ComponentGuid = new Guid(GetAttributeDecoded(nodeTemplate, "Guid"));
				compGroup.m_components.Add(ComponentId);

//				StringCollection files = new StringCollection();

				DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);

				AppendFilesAndDirectories(compGroup, ref nodeDirectory, ref nodeComponent, sourceDirInfo, ComponentGuid,
				                          sourceDirInfo.FullName);

				nodeDirectory.AppendChild(m_doc.CreateProcessingInstruction("undef", VAR_BASEDIR));

				return nodeDirectory;
			}

			/// <summary>
			/// Processing TDirectory tag
			/// </summary>
			/// <param name="compGroup">Component group</param>
			/// <param name="nodeTemplate">XML node, contained template</param>
			/// <returns>Generated XML node</returns>
			private XmlElement Process_Directory(ComponentGroup compGroup, XmlElement nodeTemplate)
			{
				XmlElement nodeDirectory = m_doc.CreateElement("Directory", m_namespaceURI);
				CopyAttribute(nodeDirectory, nodeTemplate, "Id");

				string dirName = GetAttributeDecoded(nodeTemplate, "Name");
				setDirectoryName(ref nodeDirectory, dirName);

				XmlElement nodeComponent = m_doc.CreateElement("Component", m_namespaceURI);
				nodeDirectory.AppendChild(nodeComponent);

				string ComponentId = GetAttributeDecoded(nodeTemplate, "ComponentId");
				nodeComponent.SetAttribute("Id", ComponentId);
				CopyAttribute(nodeComponent, nodeTemplate, "Guid");
				nodeComponent.SetAttribute("KeyPath", "yes");
				compGroup.m_components.Add(ComponentId);

//				StringCollection files = new StringCollection();

				foreach (XmlNode child in nodeTemplate.ChildNodes)
				{
					XmlElement node = child as XmlElement;
					if (node == null)
						continue;

					if (node.Name == "TFiles")
					{
						Process_Files(nodeComponent, node);
					}
					else if (node.Name == "TDirectory")
					{
						XmlElement nodeSubDirectory = Process_Directory(compGroup, node);
						nodeDirectory.AppendChild(nodeSubDirectory);
					}
					else if (node.Name == "Inline")
					{
						foreach (XmlNode subInline in node.ChildNodes)
						{
							XmlNode newNode = m_doc.ImportNode(subInline, true);
							nodeComponent.AppendChild(newNode);
						}
					}
					else
					{
						GenError("Invalid node name - " + node.Name);
					}
				}

				return nodeDirectory;
			}

			private static StringCollection GetFilteredFileList(DirectoryInfo sourceDirectory, ArrayList includes, ArrayList excludes)
			{
				StringCollection fileList = new StringCollection();

				foreach (FileFilter fileFilter in includes)
				{
					FileInfo[] files = sourceDirectory.GetFiles(fileFilter.Mask);

					if (files.Length <= 0)
					{
						if (fileFilter.FailOnEmpty)
							GenError("File by mask '{0}' not found in directory '{1}' !", fileFilter.Mask, sourceDirectory.FullName);
					}
					else
						for (int i = 0; i < files.Length; ++i)
						{
							string fileFullName = files[i].FullName;

							int fileIndex = fileList.IndexOf(fileFullName);
							// Защита от дублирования
							if (fileIndex < 0)
								fileList.Add(fileFullName);
							else
							{
								Console.Error.WriteLine(
									"WixGen.exe : warning WGEN1002: File '{0}' already included.\r\nDetail info: Directory '{1}', Filter '{2}'\r\n",
									fileFullName, sourceDirectory.FullName, fileFilter.Mask);
							}
						}
				}

				foreach (FileFilter fileFilter in excludes)
				{
					FileInfo[] files = sourceDirectory.GetFiles(fileFilter.Mask);

					for (int i = 0; i < files.Length; ++i)
					{
						int fileIndex = fileList.IndexOf(files[i].FullName);
						if (fileIndex >= 0)
							fileList.RemoveAt(fileIndex);
					}
				}

				return fileList;
			}

			private void Process_Files(XmlElement nodeComponent, XmlElement nodeTemplate)
			{
				DirectoryInfo sourceDir = new DirectoryInfo(GetAttributeDecoded(nodeTemplate, "SourceDir"));
				if (!sourceDir.Exists)
				{
					throw new WixGenException(String.Format("Directory '{0}' is not exist' ", sourceDir.FullName));
//					GenWarning("Directory {0} not exist !", sourceDir.FullName);
//                  return;
				}

				ArrayList attrs = new ArrayList();
				// Getting file attributes
				{
					string value;

					value = nodeTemplate.GetAttribute("DiskId");
					if (!String.IsNullOrEmpty(value))
						attrs.Add(new FileAttr("DiskId", value));

					value = nodeTemplate.GetAttribute("Vital");
					if (!String.IsNullOrEmpty(value))
						attrs.Add(new FileAttr("Vital", value));

					value = nodeTemplate.GetAttribute("Checksum");
					if (!String.IsNullOrEmpty(value))
						attrs.Add(new FileAttr("Checksum", value));
				}


				StringCollection fileList;

				{
					ArrayList includeFilters = new ArrayList();
					ArrayList excludeFilters = new ArrayList();

					if (m_defaultExcludes)
					{
						excludeFilters.Add(new FileFilter("vssver.scc"));
						excludeFilters.Add(new FileFilter("vssver2.scc"));
					}

					foreach (XmlNode child in nodeTemplate.ChildNodes)
					{
						XmlElement node = child as XmlElement;
						if (node == null)
							continue;

						if (node.Name == "Include")
						{
							string fileMask = GetAttributeDecoded(node, "Name");
							string failOnEmptyStr = "";

							if (node.HasAttribute("FailOnEmpty"))
							{
								failOnEmptyStr = GetAttributeDecoded(node, "FailOnEmpty");
								if (failOnEmptyStr != "true" && failOnEmptyStr != "false")
									GenError("Invalid value at attribute FailOnEmpty : <Include FailOnEmpty=\"{0}\">", failOnEmptyStr);
							}

							includeFilters.Add(new FileFilter(fileMask, failOnEmptyStr == "true"));
						}
						else if (node.Name == "Exclude")
						{
							string fileMask = GetAttributeDecoded(node, "Name");
							excludeFilters.Add(new FileFilter(fileMask));
						}
					}

					fileList = GetFilteredFileList(sourceDir, includeFilters, excludeFilters);
				}


				if (fileList.Count == 0)
					Console.Error.WriteLine("WixGen.exe : warning WGEN1001: No files included from directory {0}.\r\n",
					                        sourceDir.FullName);

				foreach (string FilePath in fileList)
				{
					XmlElement nodeFile = m_doc.CreateElement("File", m_namespaceURI);
					nodeComponent.AppendChild(nodeFile);

					string fileName = Path.GetFileName(FilePath);
					string fileExt = Path.GetExtension(FilePath);

					string shortFileName = m_namePrefix + "_" + m_fileCounter +
					                       (fileExt.Length > 4 ? fileExt.Substring(0, 4) : fileExt);

					StringBuilder fileId = new StringBuilder(fileName);

					for (int i = 0; i < fileId.Length; ++i)
					{
						switch (fileId[i])
						{
							case ' ':
								fileId[i] = '.';
								break;
							case '-':
							case '(':
							case ')':
								fileId[i] = '_';
								break;
						}
					}

					{
						int lenRootId = m_namePrefix.Length + 2 + fileId.Length + m_fileCounter.ToString().Length;

						if (lenRootId > 72)
						{
							fileId.Remove(0, lenRootId - 72);
						}
					}

					nodeFile.SetAttribute("Id", m_namePrefix + '_' + fileId + '.' + m_fileCounter);
					if (m_wixVersion == WixVersion.Wix3)
					{
						nodeFile.SetAttribute("Name", fileName);
					}
					else
					{
						nodeFile.SetAttribute("Name", shortFileName);
						nodeFile.SetAttribute("LongName", fileName);
					}
					nodeFile.SetAttribute("Source", FilePath);

					foreach (FileAttr fa in attrs)
					{
						nodeFile.SetAttribute(fa.name, fa.value);
					}

					m_fileCounter++;
				}

				// Process TFiles
			}

			#region Nested type: FileAttr

			private struct FileAttr
			{
				public string name;
				public string value;

				public FileAttr(string _name, string _value)
				{
					name = _name;
					value = _value;
				}
			}

			#endregion
		}

		#endregion
	}
}