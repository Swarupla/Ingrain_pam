using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Microsoft.Extensions.Options;
using Accenture.MyWizard.Shared.Helpers;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using System.Threading.Tasks;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Shared.Helpers
{
    public class EncryptionDecryption
    {


        private readonly IngrainAppSettings appSettings;
        private readonly string EncryptionKey;
        private readonly byte[] salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        private readonly int iterationCount = 1000;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        private readonly string AesKay;
        private readonly string Vector;
        private readonly bool _IsAESKeyVault;
        public EncryptionDecryption(string aes, string vector, bool IsAESKeyVault)
        {
            AesKay = aes;
            Vector = vector;
            _IsAESKeyVault = IsAESKeyVault;
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }

        public EncryptionDecryption(IngrainAppSettings settings)
        {
            AesKay = settings.aesKey;
            Vector = settings.aesVector;
            _IsAESKeyVault = settings.IsAESKeyVault;
            appSettings = settings;
            EncryptionKey = appSettings.FileEncryptionKey;
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        public string Encrypt(string ColumnValue)
        {
            try
            {
                if(_IsAESKeyVault)
                    return (CryptographyUtility.Encrypt(ColumnValue));
                else
                    return (AesProvider.Encrypt(ColumnValue, AesKay, Vector));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public string Decrypt(string ColumnValue)
        {
            try
            {
                if(_IsAESKeyVault)
                    return (CryptographyUtility.Decrypt(ColumnValue));
                else
                    return (AesProvider.Decrypt(ColumnValue, AesKay, Vector));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public string Decrypt(string ColumnValue,string aeskey , string vector)
        {
            try
            {
                return (AesProvider.Decrypt(ColumnValue, aeskey, vector));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public void EncryptFile(IFormFile fs, string outputPath)
        {
            try
            {
                if (appSettings.EncryptUploadedFiles)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(EncryptFile), "File Encryption started", string.Empty, string.Empty, string.Empty, string.Empty);
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, salt);
                    pdb.IterationCount = iterationCount;
                    byte[] Key = pdb.GetBytes(32);
                    byte[] Vector = pdb.GetBytes(16);

                    using (Aes encryptor = Aes.Create())
                    {
                        encryptor.Key = Key;
                        encryptor.IV = Vector;
                        using (FileStream fsOutput = new FileStream(outputPath, FileMode.Create))
                        {
                            using (CryptoStream cs = new CryptoStream(fsOutput, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                using (Stream fsInput = fs.OpenReadStream())
                                {
                                    fsInput.CopyTo(cs);
                                    //int data;
                                    //while ((data = fsInput.ReadByte()) != -1)
                                    //{
                                    //    cs.CopyTo(fsInput)
                                    //    cs.WriteByte((byte)data);
                                    //}
                                }
                            }
                        }
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(EncryptFile), "Encryption end", string.Empty, string.Empty, string.Empty, string.Empty);
                }
                else
                {
                    using (var fileStream = new FileStream(outputPath, FileMode.Create))
                    {
                        fs.CopyTo(fileStream);
                    }
                }
            }


            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task EncryptDataSetFile(List<FileDetails> MergeOrder, string outputPath)
        {
            try
            {
                if (appSettings.EncryptUploadedFiles)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(EncryptFile), "File Encryption started", string.Empty, string.Empty, string.Empty, string.Empty);
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, salt);
                    pdb.IterationCount = iterationCount;
                    byte[] Key = pdb.GetBytes(32);
                    byte[] Vector = pdb.GetBytes(16);

                    using (Aes encryptor = Aes.Create())
                    {
                        encryptor.Key = Key;
                        encryptor.IV = Vector;
                        using (FileStream fsOutput = new FileStream(outputPath, FileMode.Create))
                        {
                            using (CryptoStream cs = new CryptoStream(fsOutput, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                            {

                                // merge each file chunk back into one contiguous file stream  
                                foreach (var chunk in MergeOrder)
                                {
                                    using (FileStream fileChunk =
                                          new FileStream(chunk.FileName, FileMode.Open))
                                    {
                                        await fileChunk.CopyToAsync(cs);
                                    }

                                }

                                //using (Stream fsInput = fs.OpenReadStream())
                                //{
                                //    fsInput.CopyTo(cs);
                                //    //int data;
                                //    //while ((data = fsInput.ReadByte()) != -1)
                                //    //{
                                //    //    cs.CopyTo(fsInput)
                                //    //    cs.WriteByte((byte)data);
                                //    //}
                                //}
                            }
                        }
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(EncryptFile), "Encryption end", string.Empty, string.Empty, string.Empty, string.Empty);
                }
                else
                {
                    using (var fileStream = new FileStream(outputPath, FileMode.Create))
                    {
                        foreach (var chunk in MergeOrder)
                        {
                            using (FileStream fileChunk =
                                  new FileStream(chunk.FileName, FileMode.Open))
                            {
                                await fileChunk.CopyToAsync(fileStream);
                            }

                        }
                    }
                }
            }


            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }



        public void EncryptStreamFile(string inputPath, string outputPath)
        {
            try
            {
                if (appSettings.EncryptUploadedFiles)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(EncryptFile), "File Encryption started", string.Empty, string.Empty, string.Empty, string.Empty);
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, salt);
                    pdb.IterationCount = iterationCount;
                    byte[] Key = pdb.GetBytes(32);
                    byte[] Vector = pdb.GetBytes(16);

                    using (Aes encryptor = Aes.Create())
                    {
                        encryptor.Key = Key;
                        encryptor.IV = Vector;
                        using (FileStream fsOutput = new FileStream(outputPath, FileMode.Create))
                        {
                            using (CryptoStream cs = new CryptoStream(fsOutput, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                            {

                                using (FileStream fs = new FileStream(inputPath, FileMode.Create))
                                {
                                    fs.CopyTo(cs);
                                }

                                //int data;
                                //while ((data = fsInput.ReadByte()) != -1)
                                //{
                                //    cs.CopyTo(fsInput)
                                //    cs.WriteByte((byte)data);
                                //}

                            }
                        }
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(EncryptFile), "Encryption end", string.Empty, string.Empty, string.Empty, string.Empty);
                }

            }


            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void DecryptFiles(string inputFilePath, string outputfilePath)
        {
            try
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, salt);
                pdb.IterationCount = iterationCount;
                byte[] Key = pdb.GetBytes(32);
                byte[] Vector = pdb.GetBytes(16);
                using (Aes encryptor = Aes.Create())
                {
                    encryptor.Key = Key;
                    encryptor.IV = Vector;
                    using (FileStream fsInput = new FileStream(inputFilePath, FileMode.Open))
                    {
                        using (CryptoStream cs = new CryptoStream(fsInput, encryptor.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (FileStream fsOutput = new FileStream(outputfilePath, FileMode.Create))
                            {
                                int data;
                                while ((data = cs.ReadByte()) != -1)
                                {
                                    fsOutput.WriteByte((byte)data);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public void DecryptFile(Stream fs, string outputPath)
        {
            try
            {
                if (appSettings.EncryptUploadedFiles)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(DecryptFile), "File decryption started", string.Empty, string.Empty, string.Empty, string.Empty);
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, salt);
                    pdb.IterationCount = iterationCount;
                    byte[] Key = pdb.GetBytes(32);
                    byte[] Vector = pdb.GetBytes(16);
                    using (Aes encryptor = Aes.Create())
                    {
                        encryptor.Key = Key;
                        encryptor.IV = Vector;
                        using (Stream fsInput = fs)
                        {
                            using (CryptoStream cs = new CryptoStream(fsInput, encryptor.CreateDecryptor(), CryptoStreamMode.Read))
                            {
                                using (FileStream fsOutput = new FileStream(outputPath, FileMode.Create))
                                {
                                    int data;
                                    while ((data = cs.ReadByte()) != -1)
                                    {
                                        fsOutput.WriteByte((byte)data);
                                    }
                                }
                            }
                        }
                    }

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(EncryptionDecryption), nameof(DecryptFile), "Decryption end", string.Empty, string.Empty, string.Empty, string.Empty);

                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
