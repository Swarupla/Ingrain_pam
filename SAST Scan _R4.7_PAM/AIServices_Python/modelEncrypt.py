# -*- coding: utf-8 -*-
"""
Created on Wed Mar  9 16:23:51 2022

@author: maddikuntla.p.kumar
"""

import time
import configparser, os
config = configparser.RawConfigParser()
configpath = "./pythonconfig.ini"
import file_encryptor
import base64

try:
    config.read(configpath)
except UnicodeDecodeError:    
    print("****inside exception unidecode pythonconfig****")
    config = file_encryptor.get_configparser_obj(configpath)
    
    
basepath=config['config']['path']
import platform
if platform.system() == 'Linux':
    dir_pattern='/'
    
elif platform.system() == 'Windows':
    dir_pattern='\\\\'

import spacy_endecrypt as sedt


provider, key_val, vector = file_encryptor.get_from_vault(str(os.getcwd()))

if provider == 'AWS' or provider == 'Azure':
    key = key_val
    iv =  vector
    EncryptionFileKey = key_val
elif provider == "NoProvider":
    key = base64.b64decode(config['SECURITY']['Key'])
    iv =  base64.b64decode(config['SECURITY']['Iv'])
    EncryptionFileKey = config['SECURITY']['Key']


shared_model_va = config['ModelPath']['model_path_va']
#encrypt_key=config['PICKLE_ENCRYPT']['key']

#####new addition

#C:\TFS\myWizard-Ingrain-R4.2.2-UTC\AIServices_Python\intent_entity\generated\client_agile\dc_agile\bb1cb2a6-672d-4f2f-8817-f1c408871696
#C:\TFS\myWizard-Ingrain-R4.2.2-UTC\AIServices_Python\intent_entity\generated\Devops_va123\Devops_va123\43a8b78d-a118-4327-9104-253bc2f96ac6
#C:\TFS\myWizard-Ingrain-R4.2.2-UTC\AIServices_Python\intent_entity\generated\StageAD123\StageAD123\c8f7ad9b-6c95-4303-b240-ba3c4d319548
#C:\TFS\myWizard-Ingrain-R4.2.2-UTC\AIServices_Python\intent_entity\generated\VTM_Training\VTM_Training_dc\73887cee-d218-4e4c-b29b-ab2de0cb97f6


client_id1='client_agile'
dc_id1='dc_agile'
correlation_id1='bb1cb2a6-672d-4f2f-8817-f1c408871696'

va_model_path_entity1 = shared_model_va+dir_pattern +"generated"+dir_pattern+client_id1+dir_pattern+dc_id1+dir_pattern+correlation_id1+dir_pattern+"output_entity"+dir_pattern+"model-best"
va_model_path_intent1 = shared_model_va+dir_pattern+"generated"+dir_pattern+client_id1+dir_pattern+dc_id1+dir_pattern+correlation_id1+dir_pattern+"output_intent"+dir_pattern+"model-best"

"decrypting the client_agile"
# model_Object_entity=sedt.spacy_decrypt(va_model_path_entity1,EncryptionFileKey)
# model_Object_intent=sedt.spacy_decrypt(va_model_path_intent1,EncryptionFileKey)

"Encrypting the client_aglie"
sedt.spacy_encrypt(va_model_path_entity1,EncryptionFileKey)
sedt.spacy_encrypt(va_model_path_intent1,EncryptionFileKey)



client_id2='Devops_va123'
dc_id2='Devops_va123'
correlation_id2='43a8b78d-a118-4327-9104-253bc2f96ac6'

va_model_path_entity2 = shared_model_va+dir_pattern +"generated"+dir_pattern+client_id2+dir_pattern+dc_id2+dir_pattern+correlation_id2+dir_pattern+"output_entity"+dir_pattern+"model-best"
va_model_path_intent2 = shared_model_va+dir_pattern+"generated"+dir_pattern+client_id2+dir_pattern+dc_id2+dir_pattern+correlation_id2+dir_pattern+"output_intent"+dir_pattern+"model-best"

"decrypting the client_agile"
# model_Object_entity=sedt.spacy_decrypt(va_model_path_entity2,encrypt_key)
# model_Object_intent=sedt.spacy_decrypt(va_model_path_intent2,encrypt_key)

"Encrypting the client_aglie"
sedt.spacy_encrypt(va_model_path_entity2,EncryptionFileKey)
sedt.spacy_encrypt(va_model_path_intent2,EncryptionFileKey)




client_id3='StageAD123'
dc_id3='StageAD123'
correlation_id3='c8f7ad9b-6c95-4303-b240-ba3c4d319548'

va_model_path_entity3 = shared_model_va+dir_pattern +"generated"+dir_pattern+client_id3+dir_pattern+dc_id3+dir_pattern+correlation_id3+dir_pattern+"output_entity"+dir_pattern+"model-best"
va_model_path_intent3 = shared_model_va+dir_pattern+"generated"+dir_pattern+client_id3+dir_pattern+dc_id3+dir_pattern+correlation_id3+dir_pattern+"output_intent"+dir_pattern+"model-best"

# "decrypting the client_agile"
# model_Object_entity=sedt.spacy_decrypt(va_model_path_entity3,encrypt_key)
# model_Object_intent=sedt.spacy_decrypt(va_model_path_intent3,encrypt_key)

"Encrypting the client_aglie"
sedt.spacy_encrypt(va_model_path_entity3,EncryptionFileKey)
sedt.spacy_encrypt(va_model_path_intent3,EncryptionFileKey)


client_id4='VTM_Training'
dc_id4='VTM_Training_dc'
correlation_id4='73887cee-d218-4e4c-b29b-ab2de0cb97f6'

va_model_path_entity4 = shared_model_va+dir_pattern +"generated"+dir_pattern+client_id4+dir_pattern+dc_id4+dir_pattern+correlation_id4+dir_pattern+"output_entity"+dir_pattern+"model-best"
va_model_path_intent4 = shared_model_va+dir_pattern+"generated"+dir_pattern+client_id4+dir_pattern+dc_id4+dir_pattern+correlation_id4+dir_pattern+"output_intent"+dir_pattern+"model-best"

"decrypting the client_agile"
# model_Object_entity=sedt.spacy_decrypt(va_model_path_entity4,encrypt_key)
# model_Object_intent=sedt.spacy_decrypt(va_model_path_intent4,encrypt_key)

"Encrypting the client_aglie"
sedt.spacy_encrypt(va_model_path_entity4,EncryptionFileKey)
sedt.spacy_encrypt(va_model_path_intent4,EncryptionFileKey)




