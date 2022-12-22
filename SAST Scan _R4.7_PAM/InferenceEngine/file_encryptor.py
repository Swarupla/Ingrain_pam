'''
Created on Feb 17, 2020

@author: banala.u.vipin.kumar
'''
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import os, sys, json, io, time
from io import BytesIO
import pyAesCrypt as pac
from os import stat, remove
from cryptography.fernet import Fernet
import configparser
from configparser import ConfigParser, ExtendedInterpolation
import logging
import json
from logging.handlers import RotatingFileHandler
from binaryornot.check import is_binary
import boto3
import base64
from botocore.exceptions import ClientError
import json

#log_file_name = 'encrypt.log'
max_log_bytes = 5000000
backups = 5
log_format = '%(asctime)s %(levelname)s] %(message)s [%(filename)s:%(funcName)s:%(lineno)d'
# DEBUG=10, INFO=20, WARNING=30, ERROR=40, CRITICAL=50
log_level = 20
password_file = os.path.join(os.path.expanduser('~'), '.key')
json_file = 'app.json'
maxPassLen = 1024
# AES block size in bytes
AESBlockSize = 16
bufferSize = 64 * 1024
require_decrypt_file = 0


def logging_func():
    logger = logging.getLogger(__name__)
    formatter = logging.Formatter(log_format)
    #HANDLER = RotatingFileHandler(log_file_name, maxBytes=max_log_bytes, backupCount=backups)
    #HANDLER.setFormatter(formatter)

    # log to console as well
    # otherwise, StreamHandler would log to STDERR, so alter
    console_handler = logging.StreamHandler(sys.stdout)
    console_handler.setFormatter(formatter)

    #logger.addHandler(HANDLER)
    logger.addHandler(console_handler)
    logger.setLevel(log_level)
    return logger


logger = logging_func()
#logger.info("log is written to %s" % log_file_name)


def _verify_path_(path):
    path = os.path.abspath(path)
    logger.debug("verifying path: %s" % path)
    file_exists = os.path.exists(path)
    pdir = os.path.dirname(path)
    dir_exists = os.path.exists(pdir)
    logger.debug("its stats: file_exists: %s, dir_exists: %s" % (file_exists, dir_exists))
    return file_exists, dir_exists


def read_json_file(app_dot_json):
    json_dict = None
    with open(app_dot_json) as fh:
        json_dict = json.load(fh)
    logger.debug("read app.json, its content: %s" % json_dict)
    return json_dict


def get_passw_filename(app_name):
    return "." + app_name + ".key"


def _validate_app_dot_json_(json_dict):
    fextention = ".replaceme"
    err_msg = "No such file or directory: %s"
    # validate contents of app.json
    for c, d in enumerate(json_dict["files_to_encrypt"]):
        f_exists, d_exists = _verify_path_(d["input"])
        if not f_exists:
            logger.error(err_msg % d["input"])
            raise FileNotFoundError(err_msg % d["input"])
        # have abspath instead of relative path
        d["input"] = os.path.abspath(d["input"])

        if "output" in d:
            f_exists, d_exists = _verify_path_(d["output"])
            if not d_exists:
                raise FileNotFoundError(err_msg % d["output"])
            # have abspath instead of relative path
            d["output"] = os.path.abspath(d["output"])
        else:
            # user has not supplied filename for output.
            # Which means we need to override input file itself with
            # encrypted content. So create temparary file for later
            # replacement
            logger.warning("No 'output' provided, 'input' will be overwritten")
            d["output"] = os.path.abspath(d["input"]) + fextention
        # replace relative path with abspath if exists
        #logger.info("'app.json', is valid")


def create_passw_file(dir_name, fname):
    logger.debug("creating password file")
    passw_file = os.path.join(dir_name, get_passw_filename(fname))
    key = Fernet.generate_key()
    with open(passw_file, "wb") as fh:
        fh.write(key)
    logger.debug("created password file: %s" % passw_file)
    return key, passw_file



def create_passw_file_online(dir_name, fname):
    provider, key, vector = get_from_vault(dir_name)
    if provider == 'AWS':
        return key.encode(), ""
    if provider == 'Azure':
        return key.encode(), ""
    elif provider == "NoProvider":
        logger.debug("creating password file")
        passw_file = os.path.join(dir_name, get_passw_filename(fname))
        key = Fernet.generate_key()
        with open(passw_file, "wb") as fh:
            fh.write(key)
        logger.debug("created password file: %s" % passw_file)
        return key, passw_file

def get_from_vault(dir_name):
    auth_config_file = "authconfig.ini"
    fh = open(os.path.join(dir_name,auth_config_file), "r")
    cp = ConfigParser(interpolation=ExtendedInterpolation())
    cp.read_file(fh)
    if cp['Files']['iscloudkeystorage'] == 'true':
        if cp['General']['type'] == 'AWS':
            secret_name = cp['AWSSecretManagerProvider']['secretName']
            region_name = cp['AWSSecretManagerProvider']['region']
            api_version = cp['AWSSecretManagerProvider']['version']
            session = boto3.session.Session()

            client = session.client(
                service_name='secretsmanager',
                region_name=region_name
            )

            try:
                get_secret_value_response = client.get_secret_value(SecretId=secret_name,VersionStage=api_version)
            except ClientError as e:
                print (str(e))
            else:
                #print("in else")
                #print (get_secret_value_response)
                if 'SecretString' in get_secret_value_response:
                    secret = get_secret_value_response['SecretString']
                else:
                    secret = base64.b64decode(get_secret_value_response['SecretString'])
            #return json.loads(secret)
            dict_keys = json.loads(secret) 
            return "AWS" , dict_keys[cp['SecretKeyProvider']['keyName']], dict_keys[cp['SecretKeyProvider']['vectorName']]

        elif cp['General']['type'] == 'Azure':
            return "Azure" , " " ," "

    else:
        return "NoProvider", " ", " "
    #cp.set('SecretKeyProvider', 'keyName',dict_keys['Aes_Key'])
    #cp.set('SecretKeyProvider', 'vectorName',dict_keys['Aes_Vector'])
    #with open(auth_config_file, 'w') as configfile:
    #    cp.write(configfile)

def convert(app_dot_json, operation):
    fextention = ".replaceme"
    try:
        json_dict = read_json_file(app_dot_json)
        logger.debug("validating value is '%s'" % app_dot_json)
        _validate_app_dot_json_(json_dict)
        app_name = json_dict["app_name"]
        # IMP: create '.key' file in app.json file location
        location_app_dot_json = os.path.dirname(os.path.abspath(app_dot_json))
        if operation == 'encrypt':
            passw, passw_fname = create_passw_file(location_app_dot_json, app_name)
        elif operation == 'decrypt':
            logger.debug("Reading passw file")
            passw = get_encryptor_key(app_dot_json)

        # all filename-entries in app.json are valid, so start encrypting each
        for d in json_dict["files_to_encrypt"]:
            if operation == 'encrypt':
                #logger.info("creating encrypted file")
                pac.encryptFile(d["input"], d["output"], passw.decode('utf-8'), bufferSize)
                logger.debug("done")
            elif operation == 'decrypt':
                #logger.info("creating decrypted file")
                pac.decryptFile(d["input"], d["output"], passw, bufferSize)
                logger.debug("done")
            if fextention in d["output"]:
                logger.debug("renaming %s to %s" % (d["output"], d["input"]))
                os.remove(d["input"])
                os.rename(d["output"], d["input"])
                logger.debug("done")
    except IOError as e:
        logger.error(e)
        # raise IOError(e)
    except FileNotFoundError as e:
        logger.error('FileNotFoundError: %s' % str(e))
    except ValueError as e:
        logger.error(e)
        # raise ValueError(e)
    except PermissionError as e:
        logger.error(e)

def converto(app_dot_json, operation):
    fextention = ".replaceme"
    try:
        json_dict = read_json_file(app_dot_json)
        logger.debug("validating value is '%s'" % app_dot_json)
        _validate_app_dot_json_(json_dict)
        app_name = json_dict["app_name"]
        # IMP: create '.key' file in app.json file location
        location_app_dot_json = os.path.dirname(os.path.abspath(app_dot_json))
        if operation == 'encrypt':
            #get_from_vault()
            passw, passw_fname = create_passw_file_online(location_app_dot_json, app_name)
        elif operation == 'decrypt':
            logger.debug("Reading passw file")
            passw = get_encryptor_key_online(location_app_dot_json,app_dot_json)

        # all filename-entries in app.json are valid, so start encrypting each
        for d in json_dict["files_to_encrypt"]:
            if operation == 'encrypt':
                #logger.info("creating encrypted file")
                pac.encryptFile(d["input"], d["output"], passw.decode('utf-8'), bufferSize)
                logger.debug("done")
            elif operation == 'decrypt':
                #logger.info("creating decrypted file")
                pac.decryptFile(d["input"], d["output"], passw, bufferSize)
                logger.debug("done")
            if fextention in d["output"]:
                logger.debug("renaming %s to %s" % (d["output"], d["input"]))
                os.remove(d["input"])
                os.rename(d["output"], d["input"])
                logger.debug("done")
    except IOError as e:
        logger.error(e)
        # raise IOError(e)
    except FileNotFoundError as e:
        logger.error('FileNotFoundError: %s' % str(e))
    except ValueError as e:
        logger.error(e)
        # raise ValueError(e)
    except PermissionError as e:
        logger.error(e)

def decrypt_file(infile, passw):
    output = None
    with open(infile, "rb") as efh:
        decrypt_stream = BytesIO()
        input_fsize = stat(infile).st_size
        pac.decryptStream(
            efh, decrypt_stream, passw, bufferSize, input_fsize)
        bytes_stream = decrypt_stream.getvalue()
        output = bytes_stream.decode('utf-8')
    return output


def get_configparser_obj(infile):
    """
    Helper method to get 'configparser' obj from '*.ini' file. Configuration
    file can either encrypted one or plain text.
    If the configuration is encrypted, it assumes 'app.json' file in the same
    location as param:infile. Uses 'app_name' key from the file to construct
    filename of the 'key' file, containing key to decrypt.

    param: infile, absolute path of configuration file(.ini)
    """
    try:
        if is_binary(infile):
            dir_name = os.path.abspath(os.path.dirname(infile))
            app_dot_json = os.path.join(dir_name, 'app.json')
            #logger.info("%s: is encrypted. Decoding using key from %s" % (
                #infile, app_dot_json))
            #passw = get_encryptor_key(app_dot_json)
            passw = get_encryptor_key_online(dir_name,app_dot_json)
            output = decrypt_file(infile, passw)
            fh = io.StringIO(output)
        else:
            #logger.info("%s: is a plain text file" % infile)
            fh = open(infile, "r")
        cp = ConfigParser(interpolation=ExtendedInterpolation())
        logger.debug("constructed 'configparser' obj, reading configuration")
        cp.read_file(fh)
        return cp
    except ValueError as e:
        logger.exception(e)
        raise ValueError(e)
    except FileNotFoundError as e:
        logger.exception(e)
        raise FileNotFoundError(e)
    except PermissionError as e:
        logger.exception(e)
        raise PermissionError(e)


def get_encryptor_key(app_dot_json):
    app_dot_json = os.path.abspath(app_dot_json)
    dir_name = os.path.dirname(app_dot_json)
    json_dict = read_json_file(app_dot_json)
    passw_fname = get_passw_filename(json_dict['app_name'])
    with open(os.path.join(dir_name, passw_fname), "rb") as fp:
        passw = fp.read().decode('utf-8')
    return passw

def get_encryptor_key_online(dir_name,app_dot_json):
    provider, key, vector= get_from_vault(dir_name)
    if provider == 'AWS':
        return key
    if provider == 'Azure':
        return key
    elif provider == "NoProvider":
        app_dot_json = os.path.abspath(app_dot_json)
        dir_name = os.path.dirname(app_dot_json)
        json_dict = read_json_file(app_dot_json)
        passw_fname = get_passw_filename(json_dict['app_name'])
        with open(os.path.join(dir_name, passw_fname), "rb") as fp:
            passw = fp.read().decode('utf-8')
        return passw

if __name__ == '__main__':
    import argparse

    parser = argparse.ArgumentParser(
        prog='file_encryptor',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        description='''
File Encryptor
--------------
    A wrapper arround 'pyAesCrypt' python module, to encrypt/decrypt
    list of files supplied via 'app.json' input file. 
    --------------------
    'app.json' structure
    --------------------
    {
        "app_name": "APP_NAME",
        "files_to_encrypt":[
            { 
                "input": "/path/to/file_2b_encrypted",
                "output: "/path/of/encrypted_file",
            },
            ...
        ]
    }

    Note:
    -----
    - "output" is optional, if absent, "input" file will be overwritten with 
        encrypted content
    - "app_name", any valid one-word string
'''
    )
    #parser.add_argument(
    #    '-e', '--encrypt', dest='in_encrypt', metavar='app_dot_json',
    #    help="'app.json', a JSON file containing files to be encrypted",
    #    nargs=1, default=None)
    #parser.add_argument(
    #    '-d', '--decrypt', dest='in_decrypt', metavar='app_dot_json',
    #    help="'app.json', a JSON file containing files to be decrypted. \
    #            Mostly the same file that was supplied with '-e' option. \
    #            If 'output' was used in 'app.json', you will want to swap\
    #            'input' & 'output' key values.",
    #    nargs=1, default=None)
    parser.add_argument(
        '-e', '--encrypt', dest='in_encrypt', metavar='app_dot_json',
        help="'app.json', a JSON file containing files to be encrypted",
        nargs=1, default=None)
    parser.add_argument(
        '-d', '--decrypt', dest='in_decrypt', metavar='app_dot_json',
        help="'app.json', a JSON file containing files to be decrypted. \
                Mostly the same file that was supplied with '-e' option. \
                If 'output' was used in 'app.json', you will want to swap\
                'input' & 'output' key values.",
        nargs=1, default=None)
    args = parser.parse_args()
    #if args.in_encrypt:
    #    convert(args.in_encrypt[0], 'encrypt')
    #elif args.in_decrypt:
    #    convert(args.in_decrypt[0], 'decrypt')
    if args.in_encrypt:
        converto(args.in_encrypt[0], 'encrypt')
    elif args.in_decrypt:
        converto(args.in_decrypt[0], 'decrypt')
    else:
        parser.print_help()

    # if len(sys.argv) > 1:
    #     encrypt(sys.argv[1])
    #     print("Encryption Done")
    # else:
    #     print("Supply 'app.json' as arg to encrypt")

    # encrypt('app.json')

    # passw = get_encryptor_key('app.json')
    # # conf = get_configparser_obj('txt_input.txt.aes', passw)
    # conf_path = os.path.abspath('ai_core_configs.ini')
    # logger.info(conf_path)
    # conf = get_configparser_obj('ai_core_configs.ini_bkp')
    # print(conf.get("security", "audience"))

# TODO:
# log in both file & console
# unit testing
# PermissionError,
# app_name with invlid string(filename)
# encrypted file to encrypt
# decrypted file to decrypt
