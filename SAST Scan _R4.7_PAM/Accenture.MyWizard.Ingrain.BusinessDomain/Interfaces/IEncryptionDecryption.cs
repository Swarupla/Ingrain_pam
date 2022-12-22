using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IEncryptionDecryption
    {
        string Encrypt(string ColumnValue);
        string Decrypt(string ColumnValue);

        void EncryptFile(IFormFile fs, string outputPath);

        void DecryptFile(Stream fs, string outputPath);

        void EncryptStreamFile(string fs, string outputPath);

        Task EncryptDataSetFile(List<FileDetails> MergeOrder, string outputPath);

        void DecryptFiles(string inputFilePath, string outputfilePath);

        dynamic DecryptFiletoStream(string inputFilePath, string outputfilePath);
        string EncryptAESVaultKey(string ColumnValue);
        string DecryptAESVaultKey(string ColumnValue);

    }
}
