using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.Utility
{
    class AppSettings
    {
		public string connectionString { get; set; }
		public string MonteCarloConnection { get; set; }
		public string IEConnectionString { get; set; }
		public string certificatePath { get; set; }
		public string certificatePassKey { get; set; }
		public string FileEncryptionKey { get; set; }
		public int IterationCount { get; set; }
		public int ModelsProcessCount { get; set; }
		public String aesKey { get; set; }
		public String aesVector { get; set; }
		public String aesKeyNew { get; set; }
		public String aesVectorNew { get; set; }
		public IEnumerable<DbCollection> DbCollections { get; set; }
		public IEnumerable<DbCollection> MonteCarlodbCollections { get; set; }
		public IEnumerable<DbCollection> AIServicedbcollections { get; set; }
		public IEnumerable<DbCollection> ClusteringDbColections { get; set; }
		public IEnumerable<DbCollection> InfereneEngineDBCollections { get; set; }
		public bool IsAESKeyVault { get; set; }
	}
	public class DbCollection
	{
		public string Name { get; set; }
		public string Attributes { get; set; }
	}
}
