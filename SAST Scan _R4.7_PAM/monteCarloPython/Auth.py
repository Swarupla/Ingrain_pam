# -*- coding: utf-8 -*-
#@author: a.gaffar

import jwt
from jwt.algorithms import RSAAlgorithm
import base64
import json
import requests
import platform
import configparser, os
import sys,os
import file_encryptor
conf_path = '/pythonconfig.ini'

config = configparser.RawConfigParser()
configpath = str(os.getcwd())+str(conf_path)

try:
        config.read(configpath)
        #print(config["azure_ad"])
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)
        #config.read(configpath) 

AZURE_AD = 'AzureAD'
FORTRESS = 'Form'
AUTH_TYPE = config['auth_scheme']['authProvider']
SECRET_KEY = config["security"]["clientSecret"]
AUDIENCE = config["security"]["audience"]
ISSUER = config["security"]["issuer"]
OPEN_ID_URL = config["security"]["open_id_url"]
TENANT_ID = config["security"]["tenantId"]
OPEN_ID_URL = OPEN_ID_URL.replace("{tenant}", TENANT_ID)
ALGORITHMS = config["security"]["algorithms"].split(
        config["others"]["value_separator"])

def _get_kid_(token):
    
    """
    'kid' Specifies the thumbprint for the public key that's used to 
    sign this token. 'kid' is encoded in header section of the token"""
    # 3 parts of standard JWT 
    (header, payload, signature) = token.split(".")
    # d_header is byte-string
    d_header = base64.standard_b64decode( header )
    str_header = str(d_header, 'utf-8')
    json_header = json.loads(str_header)
    return json_header["kid"]
    

def _get_jwks_uri_():

    """
    URL for publicly available keys
    """
    resp = requests.request(url=OPEN_ID_URL, method="GET",verify=False).json()
    jwks_uri = resp["jwks_uri"]
    return jwks_uri

def validate_azure_ad_token(token):

    """
    refer documentation from https://docs.microsoft.com/en-us/azure/active-directory/develop/access-tokens#validating-tokens
    step 1: get thumbprint(kid value)
    step 2: get URL for public signing key from a standard URL with CLIENT_ID
    step 3: get public signing from step 2 url
    step 4: find entry which matches kid in the output
    step 5: form public key from various fields
    step 6: decode token using public token
    """
    (kid, jwks_uri) = _get_kid_(token), _get_jwks_uri_()
    # get azure issued public signing keys for the client
    azr_public_sign_keys = requests.request(url=jwks_uri, method="GET",verify=False).json()
    sign_key = None
    key_json=None
    for key in azr_public_sign_keys["keys"]:
        if kid == key["kid"]:
            key_json = '''{ \
                "kty":"%s", \
                "alg":"RS256", \
                "use":"%s", \
                "kid":"%s", \
                "n":"%s", \
                "e":"%s" \
                }''' %(key['kty'], key['use'], key['kid'], key['n'], key['e'])

    #logger.debug(key_json)
    public_key = RSAAlgorithm.from_jwk(key_json)
    decoded_token = jwt.decode(token, public_key, algorithms='RS256',
            audience=AUDIENCE, issuer=ISSUER)
    return decoded_token

def decode_token(token):

    """ 
    Decrypts JWT Token using secret_key and validate authenticity 
    using audience, issuer. All three are read from ai_core_config.ini 
    file, which is deployment specific. secret_key is aslo a 'base64' 
    encoded string.

    param: token, a mywizard-phoenix issued token
    returns: decoded json from token

    this method doesn't supress any exceptions that jwt.decode() throws,
    becuase consumer can handle the way they want. for list all exceptions,
    read documentation at: https://pyjwt.readthedocs.io/en/latest/api.html
    """
    
    if AUTH_TYPE == AZURE_AD:
        decoded_jwt = validate_azure_ad_token(token)
    elif AUTH_TYPE == FORTRESS:
        decoded_jwt = jwt.decode(
            token,
            base64.standard_b64decode( SECRET_KEY ),
            algorithms=ALGORITHMS,
            verify=True,
            audience=AUDIENCE,
            issuer=ISSUER,
            ) 
    return decoded_jwt

def validateToken(token):
    try:
      decode_token(token)
      return "Success"
    except:
      return "Fail"
