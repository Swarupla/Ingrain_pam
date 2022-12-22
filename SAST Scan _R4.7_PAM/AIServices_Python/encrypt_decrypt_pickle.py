from cryptography.fernet import Fernet
import configparser

#configpath = './pythonconfig.ini'
#config = configparser.RawConfigParser()
#config.read(configpath)   

import file_encryptor
config = configparser.RawConfigParser()
configpath = './pythonconfig.ini'
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
  
key = config['PICKLE_ENCRYPT']['key']

def load_key():
    """
    Loads the key from the current directory named `key.key`
    """
    return key



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
