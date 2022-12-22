
import pickle
from SSAIutils import utils
from SSAIutils import EncryptData
import platform
from os import listdir
from os.path import isfile, join

if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'
import configparser, os

mainPath = os.getcwd() + work_dir
import sys

sys.path.insert(0, mainPath)
sys.path.insert(0, mainPath + '/main')
import file_encryptor
config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)

#provider, newkey, vector = file_encryptor.get_from_vault(configpath)
    
saved_model_path = config['filePath']['saveModelPath']
mnt_path = saved_model_path.split("SavedModels")[0] + 'DataSets/'
print(mnt_path)
not_encrypted_files_full_name = [f for f in listdir(mnt_path) if isfile(join(mnt_path, f)) and  not f.endswith(".enc")]
not_encrypted_files = [f.split("_")[0] for f in listdir(mnt_path) if isfile(join(mnt_path, f)) and  not f.endswith(".enc")]
#print(not_encrypted_files)
uniq_corrs = set(not_encrypted_files)
#uniq_corrs = ['43f672f8-6958-4330-ab8f-b6c4dcd12422']
if len(uniq_corrs)>0:
    for corr in uniq_corrs:
        dbconn, dbcollection = utils.open_dbconn("DataSetInfo")
        dbcollection.remove({"DataSetUId": corr})
        dbconn, dbcollection = utils.open_dbconn("DataSet_IngestData")
        dbcollection.remove({"DataSetUId": corr})
        files_t_delete = [f for f in not_encrypted_files_full_name if f.__contains__(corr) and  not f.endswith(".enc")]
        for file in files_t_delete:
            os.remove(os.path.join(mnt_path,file))
    output_file = open('datasets_removed.txt', 'w')
    for corr in uniq_corrs:
        output_file.write(corr)
        output_file.write("\n")
    output_file.close()