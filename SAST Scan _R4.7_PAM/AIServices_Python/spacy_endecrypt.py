import configparser, os
config = configparser.RawConfigParser()
configpath = "./pythonconfig.ini"
import file_encryptor
configpath = "./pythonconfig.ini"
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
#encrypt_key=config['PICKLE_ENCRYPT']['key']
import glob
import os
import spacy
from cryptography.fernet import Fernet
import platform

if platform.system()=='Linux':
    dir_pattern='/'
else:
    dir_pattern='\\\\'

def encrypt(filename, key):
    """
    Given a filename (str) and key (bytes), it encrypts the file and write it
    """
    f = Fernet(key)

    with open(filename, "rb") as file:
        # read all file data
        file_data = file.read()

  # encrypt data
    encrypted_data = f.encrypt(file_data)

    # write the encrypted file
    with open(filename, "wb") as file:
        file.write(encrypted_data)

def decrypt(filename, key):
    """
    Given a filename (str) and key (bytes), it decrypts the file and write it
    """
    print("inside decrypt")
    f = Fernet(key)
    with open(filename, "rb") as file:        
    # read the encrypted data
        encrypted_data = file.read()
    # decrypt data
    decrypted_data = f.decrypt(encrypted_data)
    # write the original file
    with open(filename, "wb") as file:
        file.write(decrypted_data)



def spacy_encrypt(file_path,encrypt_key):
    
    files = [f for f in glob.glob(file_path+dir_pattern+'*', recursive=True)]
    for file_list in files:
        if os.path.isdir(file_list):
            #seems to be directory
            inf=[f for f in glob.glob(file_list+dir_pattern+'*',recursive=True)]
            for inner_f in inf:
                encrypt(inner_f,encrypt_key)
                print('encryption complete',inner_f)
            print(inf)
        else:
            encrypt(file_list,encrypt_key)
            print(file_list)
            print('encryption complete',file_list)
    print(files)


def spacy_decrypt(file_path,encrypt_key):
    files = [f for f in glob.glob(file_path+dir_pattern+'*', recursive=True)]
    for file_list in files:
        if os.path.isdir(file_list):
            #seems to be directory
            inf=[f for f in glob.glob(file_list+dir_pattern+'*')]
            for inner_f in inf:
                decrypt(inner_f,encrypt_key)
                print('decryption complete',inner_f)
            print(inf)
        else:
            decrypt(file_list,encrypt_key)
            print(file_list)
            print('dencryption complete',file_list)
    nlp=spacy.load(file_path)
    return nlp

def test():
    file_path=[]
    for file_paths in file_path:
        print(file_paths)
        #spacy_encrypt(file_paths,encrypt_key)
#spacy_decrypt(file_path,encrypt_key)


