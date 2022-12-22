# -*- coding: utf-8 -*-
"""
Created on Fri Mar  4 19:53:20 2022

@author: harsh.nandedkar
"""

newkey="ANjSCkmQhGuEUgW5hupPY20pnN6155t4nJt5r9vzDHs="
import pickle
from SSAIutils import utils
from SSAIutils import EncryptData
import platform
from cryptography.fernet import Fernet

if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'
    # pylogs_dir = '\IngrAIn_Python\PyLogs\\'
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
    
saveModelPath = config['filePath']['saveModelPath']

def decryptPickle(filepath):
    if platform.system() == 'Windows':
        file = open(bytes(filepath, "utf-8").decode("unicode_escape"), 'rb')
    else:
        file = open(filepath, 'rb')
    loadedPickle = pickle.load(file)
    f = Fernet((bytes(newkey,"utf-8")))
    model = pickle.loads(f.decrypt(loadedPickle))
    return model

filepath='/mnt/myWizard-Phoenix/IngrAIn_Shared/SavedModels/2c8ae903-8981-4762-9d74-c30a4358672a_Gradient Boost Regressor.pickle'
model_load=decryptPickle(filepath)

print(model_load.best_params_)