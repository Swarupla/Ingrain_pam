# -*- coding: utf-8 -*-
"""
Created on Thu May 28 11:52:48 2020

@author: s.siddappa.dinnimani
"""

import configparser, os
import file_encryptor
import platform
config = configparser.RawConfigParser()
conf_path = "/pythonconfig.ini"
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    #print("Decrypting Config File : Sensitivity.py")
    config = file_encryptor.get_configparser_obj(configpath)

provider, key_val, vector = file_encryptor.get_from_vault(str(os.getcwd()))

from Crypto.Cipher import AES
#from pkcs7 import PKCS7Encoder
import base64
#from cryptography.hazmat.primitives import hashes
#from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
#from cryptography.hazmat.backends import default_backend
global iv,key,mode

if provider == 'AWS' or provider == 'Azure':
    key = base64.b64decode(key_val)
    iv =  base64.b64decode(vector)
    EncryptionFileKey = key_val
elif provider == "NoProvider":
    key = base64.b64decode(config['SECURITY']['Key'])
    key_val = key
    iv =  base64.b64decode(config['SECURITY']['Iv'])
    vector = iv
    EncryptionFileKey = config['SECURITY']['Key']

#mode = AES.MODE_CBC
#key = base64.b64decode(config['SECURITY']['Key'])
#iv =  base64.b64decode(config['SECURITY']['Iv'])
#EncryptionFileKey = config['SECURITY']['FileKey']
mode = eval(config['SECURITY']['mode'])

##AES.block_size = 16
#AES.key_size =  16

def pad(text):
    return text + (AES.block_size - len(text) % AES.block_size ) * chr(AES.block_size  - len(text) % AES.block_size )

#def unpad(text):
#    return text[0:-ord(text[-1])]    

#Encrypting
def EncryptIt(message):   
    obj = AES.new(key,mode,iv)
    #    cipher_text = base64.b64encode(obj.encrypt(PKCS7Encoder().encode(message)))    
    if platform.system() == 'Linux':
        text = pad(message).encode('utf-8')
    elif platform.system() == 'Windows':
        text = pad(message).encode('latin-1')    
    cipher_text = base64.b64encode(obj.encrypt(text))    
    cipher_text = str(cipher_text)[2:-1]   
    # cipher_text = str(cipher_text).lstrip("'b")   
    # cipher_text = str(cipher_text).rstrip("'")    
    return cipher_text

#Decrypting
def DescryptIt(message):
    obj2 = AES.new(key,mode,iv)
    dtext = obj2.decrypt(message)
    dtext = dtext[:-dtext[-1]].decode('latin-1')
    return dtext

