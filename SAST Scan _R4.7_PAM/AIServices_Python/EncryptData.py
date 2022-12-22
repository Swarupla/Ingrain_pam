# -*- coding: utf-8 -*-
"""
Created on Thu May 28 11:52:48 2020

@author: s.siddappa.dinnimani
"""
import configparser, os
import sys


from Crypto.Cipher import AES
#from pkcs7 import PKCS7Encoder
import base64
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.backends import default_backend
global iv,key,mode
import file_encryptor
import platform
if platform.system() == 'Linux':
    configpath = './pythonconfig.ini'
elif platform.system() == 'Windows':
    #configpath = os.getcwd()+'\\pythonconfig.ini'

    from pathlib import Path
    mainDirectory = str(Path(__file__).parent.parent.absolute())
    #configpath = mainDirectory+"/pythonconfig.ini"

    configpath = str(os.getcwd())+'/pythonconfig.ini'
config = configparser.RawConfigParser()
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)

provider, key_val, vector = file_encryptor.get_from_vault(str(os.getcwd()))

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
    #cipher_text = str(cipher_text).lstrip("'b")   
    #cipher_text = str(cipher_text).rstrip("'")    
    return cipher_text

#Decrypting
def DescryptIt(message):
    obj2 = AES.new(key,mode,iv)
    dtext = obj2.decrypt(message)
    dtext = dtext[:-dtext[-1]].decode('latin-1')
    return dtext

#file Descryption
def decryptFile(text):
    try:
        if text is None:
            return
        else:          
            backend = default_backend()
            salt = bytes([0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76])
            kdf = PBKDF2HMAC(algorithm=hashes.SHA1(),length=48,salt=salt,iterations=1000,backend=backend)
            if provider == "NoProvider":
                key_parts = kdf.derive(bytes(EncryptionFileKey, 'utf-8'))
                k = key_parts[0:32]
                _iv = key_parts[32:]
            else:
                key_parts = kdf.derive(bytes(EncryptionFileKey, 'utf-8'))
                k = base64.b64decode(key_val)# key_parts[0:32]
                _iv = base64.b64decode(vector) #key_parts[32:]
            cipher = AES.new(k, AES.MODE_CBC, _iv)
            output = cipher.decrypt(text)
            output = output[:-output[-1]]
            return output
    except Exception as err:
        print(err) 