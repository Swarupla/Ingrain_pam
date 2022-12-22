import configparser,os
#import file_encryptor

config = configparser.RawConfigParser()
configpath = "main/pythonconfig.ini"
try:
        config.read(configpath)
        print ("False")
except UnicodeDecodeError:
        print ("True")
