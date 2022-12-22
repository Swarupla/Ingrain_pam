#Created by Joydeep Sarkar
#Created on 14-Dec-2018
#Copyright MyWizard VA
import os
import json
import sys
import requests
from flask import Flask, jsonify, request, render_template
from pathlib import Path
from shared.xlsx_helper import XLSXHelper
import xlrd
import configparser
import time
from threading import Thread
from multiprocessing import Process
import subprocess
from rasa.nlu.model import Interpreter
from rasa.model import unpack_model
from rasa.train import train_nlu,train
import pandas as pd
import numpy as np
import inflect
from nltk.stem import WordNetLemmatizer
from nltk.corpus import wordnet
from nltk.corpus import stopwords
stop = stopwords.words('english')
#from Auth import validateToken
import platform
if platform.system() == 'Linux':
    pass																																		
elif platform.system() == 'Windows':
    from requests_negotiate_sspi import HttpNegotiateAuth
from shared.logger_helper import GenerateLogger
log_generator = GenerateLogger()
currentDirectory_logger = str(Path(__file__).parent.parent.absolute())
logger = log_generator.generate_logger(currentDirectory_logger + "/intent_entity.log")
mainDirectory = str(Path(__file__).parent.parent.parent.absolute())
##print("Main directory path - "+mainDirectory)
sys.path.insert(0, mainDirectory)
from encrypt_decrypt_pickle import load_key,decrypt,encrypt
from DBProvider import updateModelStatus
import file_encryptor
configpath_model = '/app/pythonconfig.ini'
config = configparser.RawConfigParser()
#config.read(configpath_model)     
try:
    config.read(configpath_model)
except UnicodeDecodeError:
    #print("****inside exception unidecode****")
    config = file_encryptor.get_configparser_obj(configpath_model)

shared_model = config['ModelPath']['model_path']
shared_model_va = config['ModelPath']['model_path_va']
flag_synonym = config['TrainingFlag']['flag_synonym']
auth_type = config['auth']['authProvider']
import glob
import utils
#import xlsxwriter
from cryptography.fernet import Fernet

app = Flask(__name__)

#print(currentDirectory_logger)
stop.append('also')
stop.append('well')
stop.append('as')
#function to remove stopwords from query
def remove_stopwords_new(query):
    out_query=""
    for word in query.split():
        if word not in (stop):
            out_query = out_query + word +" "
    return out_query

def get_synonym(extracted_df):
    
    for row in extracted_df.itertuples():
        synonyms = dict()
        final_list=[]
        res = row.Text.split()  
        row_value = ""                
        if (type(row.Value) is not str):                   
            #print("**********before conversion",type(row.Value)) 
            row_value = str(row.Value)                 
            #print("***********after conversion",type(row_value))         
        for i,val in enumerate(res):
            if row_value not in (None, ""):
                ent_val = row_value.split(',')
                flag = 0
                for ent in ent_val:
                    if val == ent:
                        flag = 1
                if flag == 1:
                    continue
            synonyms_list=[]
            for syn in wordnet.synsets(val):
                for l in syn.lemmas():
                    synonyms_list.append(l.name())
                #print("**********while generating synonyms",row_value)
            synonyms[val]=list(set(synonyms_list[1:5]))

 

        str1 = ""
        generated_dict = dict()
        for word in res:  
            str1 += word + " "
        for i, val in enumerate(res):
            if row_value not in (None, ""):
                ent_val = row_value.split(',')
                flag = 0
                for ent in ent_val:
                    if val == ent:
                        flag = 1
                if flag == 1:
                    continue
            for synonym in synonyms[val]:
                #print("**********while generating synonym sentences",row_value)
                str_tmp = str1
                str_tmp = str_tmp.replace(val, synonym)
                #print(str_tmp)
                generated_dict["Text"] = str_tmp
                generated_dict["Intent"] = row.Intent
                generated_dict["Value"] = row_value
                generated_dict["Entity"] = row.Entity
                #print("before checking for empty values")
                if (row_value in (None, "","nan") and row.Entity not in (None, "","nan")):
                    continue
                #print("after checking for empty values")
                value_num = len(row_value.split(","))
                entity_num = len(row.Entity.split(","))
                if value_num != entity_num:
                    continue
                str_tmp = list(str_tmp.split(" "))
                ent_val = row_value.split(',')
                flag = 0
                for ent in ent_val:
                    if ent in str_tmp:
                        flag = 1
                if flag == 1:
                    extracted_df = extracted_df.append(generated_dict, ignore_index=True)
    #print("extracted_df")
    #print(extracted_df)
    return extracted_df                

# function to generate singular/plural

def get_singular_plural(data):
    '''    
    data : Original data
    returns data_new after converting to singular/plural values
    '''
    

    def isplural(word):
        
        wnl = WordNetLemmatizer()

        lemma = wnl.lemmatize(word, 'n')
        plural = True if word is not lemma else False
        return plural, lemma

    data_new = pd.DataFrame()   # Will store new rows of changed data
    p = inflect.engine()
    for _, row in data.iterrows():
#             row = data.copy().iloc[i]
            #print("before generating plural")
            row.Value = str(row.Value)
            vals = row.Value.split(',')
            #print("after generating plural")

            vals_new = list()
            temp_row = row.copy()
            for val in vals:
                flag, val_converted = isplural(val)   # Return True if plural and singular value
                if flag:
                    vals_new.append(val_converted)
                else:
                    val_converted = p.plural(val)   # If word doesnt have proper plural value, it returns False
                    vals_new.append(val_converted)
#                     if val_converted != False:   
#                         vals_new.append(val_converted)
#                     else:
#                         continue
# #                         val_converted = p.plural(val)
# #                         vals_new.append(val_converted)                        

                # Finding and replacing row text
                temp_row.Text = temp_row.Text.replace(val, vals_new[-1])

            # Updating row value after joining changed values
            temp_row.Value = ','.join(vals_new)


            # Adding synthetic new row in new dataframe
            data_new = data_new.append(temp_row, ignore_index=True)
    data_new = data_new[['Text', 'Intent', 'Value', 'Entity']]
    final_data = data.append(data_new).reset_index(drop=True)
    
    return final_data

# function to remove stopwords
def remove_stopwords(data):
    data['Text'] = data['Text'].apply(lambda x: ' '.join([word for word in x.split() if word not in (stop)]))
    return data

# API for detecting intent and entity using nlp
@app.route('/ainlp/parsetext', methods=['POST'])
def parse_text():   
    response={}
    query = ""
    data = request.get_json()  
    #print(type(data))
    #print('in parse text')
    resp = ""
    try:

        currentDirectory = str(Path(__file__).parent.parent.absolute())

        try:
            #print(len(data['client_id'])) 
            #print(len(data["query"]))  
            #print(len(data['dc_id']))  
            #print(data["model_name"])  
            #print(len(data["correlation_id"]))    
            
            if "query" not in data or len(data["query"])<2:
                #print('query  empt')
                raise ValueError('query is not provided')
            ##print("cond1")
            elif ('client_id'not  in data or len(data['client_id'])<2):
                #print('client id empt')
                raise ValueError('client_id is not provided')
            ##print("cond2")
            elif ('dc_id' not  in data or len(data['dc_id'])<2):
                raise ValueError('dc_id is not provided')
            ##print("cond3")
            elif ('model_name' not in data or len(data["model_name"])<2 ):
                raise ValueError('model_name is not provided')
            ##print("cond4")
            elif ('correlation_id' not in data or len(data["correlation_id"])<2 ):
                raise ValueError('correlation_id is not provided')
           
        except Exception as ex:
            response["message"] = str(ex)
            #print(str(ex))
            response["is_success"] = False
            logger.error(str(ex))
            return jsonify(response)
                
        
        
        folder = "generated"
        query = data["query"]
        query=query.encode('ascii',"ignore").decode('ascii')
        client_id=data['client_id']
        dc_id=data['dc_id']
        model_name=data["model_name"]
        correlation_id=data["correlation_id"]
        query = remove_stopwords_new(query)
        
        
        if (client_id == 'StageAD123' or client_id == 'client_agile' or client_id == 'Devops_va123' or client_id=="VTM_Training"):
            shared_model_path = shared_model_va
        else:
            shared_model_path = shared_model

        key = load_key()
        model_path_decrypt = shared_model_path+"/"+folder+"/"+(client_id)+("/")+(dc_id)+("/")+(correlation_id)+("/output/")+(model_name)
        files = [f for f in glob.glob(model_path_decrypt + "/nlu/*", recursive=True)]
        for f in files:
            #print(f)
            decrypt(f, key)
        files1 = [f for f in glob.glob(model_path_decrypt + "/*.json", recursive=True)]
        for f in files1:
            #print(f)
            decrypt(f, key)
      
        model_path = shared_model_path+"/"+folder+"/"+(client_id)+("/")+(dc_id)+("/")+(correlation_id)+("/output/")+(model_name)+("/nlu")
        interpreter_custom = Interpreter.load(model_path)
        
        resp = interpreter_custom.parse(query)
        #print('resp',resp)
        if (resp!=None):
            response["message"] = ""
            response["result"] = resp
            response["is_success"] = True
        else:
            response["message"] = 'Unexpected error Contact Administrator'
            response["result"] = ""
            response["is_success"] = False

        files = [f for f in glob.glob(model_path_decrypt + "/nlu/*", recursive=True)]
        for f in files:
            #print(f)
            encrypt(f, key)
        files1 = [f for f in glob.glob(model_path_decrypt + "/*.json", recursive=True)]
        for f in files1:
            #print(f)
            encrypt(f, key)

    except Exception as ex:
        response["message"] = str(ex)
        #print(str(ex))
        response["is_success"] = False
        logger.error(str(ex))
        return jsonify(response)
    ##print('response',response)
    return response

def parse_text_from_Object(modelObject):   
    response={}
    query = ""
    data = request.get_json()  
    #print(type(data))
    #print('in parse text')
    resp = ""
    try:
        currentDirectory = str(Path(__file__).parent.parent.absolute())
        try:
            #print("before model object is none---------2")
            if "query" not in data or len(data["query"])<2:
                #print('query  empt')
                raise ValueError('query is not provided')
            elif ('client_id'not  in data or len(data['client_id'])<2):
                #print('client id empt')
                raise ValueError('client_id is not provided')
            elif ('dc_id' not  in data or len(data['dc_id'])<2):
                raise ValueError('dc_id is not provided')
            elif ('model_name' not in data or len(data["model_name"])<2 ):
                raise ValueError('model_name is not provided')
            elif ('correlation_id' not in data or len(data["correlation_id"])<2 ):
                raise ValueError('correlation_id is not provided')
           
        except Exception as ex:
            response["message"] = str(ex)
            #print(str(ex))
            response["is_success"] = False
            logger.error(str(ex))
            return jsonify(response)
        #print("before model object is none---------1")
        folder = "generated"
        query = data["query"]
        client_id=data['client_id']
        dc_id=data['dc_id']
        model_name=data["model_name"]
        correlation_id=data["correlation_id"]
        #print("before model object is none")
        #print(modelObject)
        
        if modelObject is None:
            model_obj_val="model object is none"
            #modelObject = load_model(client_id, dc_id, correlation_id, model_name)
        #print("model object is not null", query)
        try:
            #print("inside try", modelObject)
            resp = modelObject.parse(query)
        except Exception as ex:        
            #print(str(ex))
            error_value=ex.args

        #print('resp',resp)
        if (resp!=None):
            response["message"] = ""
            response["result"] = resp
            response["is_success"] = True
            #print("before assign model object")
            response["model_object"]= modelObject
            #print("after assign model object")	
        else:
            response["message"] = 'Unexpected error Contact Administrator'
            response["result"] = ""
            response["is_success"] = False
            response["model_object"]= None      

    except Exception as ex:
        response["message"] = str(ex)
        #print(str(ex))
        response["is_success"] = False
        logger.error(str(ex))
        return jsonify(response)
    #print('response in parse',response)
    return response

def load_model_from_path(model_path):    
    ##print('in load model path', model_path)    
    try:
        key = load_key()
        files = [f for f in glob.glob(model_path + "/nlu/*", recursive=True)]
        for f in files:
            ##print(f)
            decrypt(f, key)
        files1 = [f for f in glob.glob(model_path + "/*.json", recursive=True)]
        for f in files1:
            ##print(f)
            decrypt(f, key)
      
        model_path_load = model_path+("/nlu")
        ##print("model_path_load", model_path_load)
        interpreter_custom = Interpreter.load(model_path_load)        
        ##print("inside load model")
        ##print(interpreter_custom)
        resp = interpreter_custom.parse("test")
        ##print("resp", resp)
        files = [f for f in glob.glob(model_path + "/nlu/*", recursive=True)]
        for f in files:
            ##print(f)
            encrypt(f, key)
        files1 = [f for f in glob.glob(model_path + "/*.json", recursive=True)]
        for f in files1:
            ##print(f)
            encrypt(f, key)
        return interpreter_custom
    except Exception as ex:        
        ##print(str(ex))
        return None
    
def train_nlp():
    response = {}
    payload = request.form['payload']
    payload_json = json.loads(payload)
    folder = "generated"
    logger.info("In training method")
    #print(payload_json)
   
    
    currentDirectory = str(Path(__file__).parent.parent.absolute())
    
    config_path = currentDirectory+'/src/config.yml'
    try:
             
        try:  
            if('client_id' not  in payload_json or len(payload_json['client_id'])<2):
                #print('client id empt')
                raise ValueError('client_id is not provided')
            elif ('dc_id' not  in payload_json or len(payload_json['dc_id'])<2):
                raise ValueError('dc_id is not provided')
            elif ('correlation_id' not  in payload_json or len(payload_json['correlation_id'])<2):
                raise ValueError('correlation_id is not provided')
            elif('DataSource' not in payload_json):
                raise ValueError('DataSource is not provided')             
            

            
        except Exception as ex:
            response["message"] = str(ex)
            #print(str(ex))
            response["is_success"] = False
            return jsonify(response)
            logger.error(str(ex))
        client_id=payload_json['client_id']
        dc_id=payload_json['dc_id']
        correlation_id=payload_json['correlation_id']
        #AppId=""#payload_json['ApplicationId']
        #data_source="File"#payload_json['DataSource']
        AppId=payload_json['ApplicationId']
        data_source=payload_json['DataSource']
        if(AppId!=""):
            #print("Getting token from Azure Application")
            try:
                if auth_type == 'AzureAD':
                    customurl_token,status_code=utils.CustomAuth(AppId)
                elif auth_type == 'WindowsAuthProvider':
                    customurl_token=''
                    status_code=200
            except Exception as e:
                #print("Correct Azure application id is not provided")
                raise ValueError('Correct Azure application id is not provided')
            #customurl_token,status_code=utils.CustomAuth(AppId)
        else:
            #Getting token for form authentication
            #print("Getting token for form authentication")
            if auth_type == 'AzureAD':
                AdTokenUrl = config['Entity']['AdTokenUrl']
                grant_type1 = config['Entity']['grant_type']
                client_id1 = config['Entity']['client_id']
                resource1 = config['Entity']['resource']
                client_secret1 = config['Entity']['client_secret']
                data_token= {'grant_type': grant_type1,'client_id' : client_id1,'resource' : resource1,'client_secret' : client_secret1}
                result_token=requests.post(AdTokenUrl,data=data_token)
                ##print("result token------------",result_token.text)
                x=result_token.json()
                customurl_token=x["access_token"]
            elif auth_type == 'WindowsAuthProvider':
                    customurl_token=''
        
        
        if (client_id == 'StageAD123' or client_id == 'client_agile' or client_id == 'Devops_va123' or client_id == 'VTM_Training'):
            #shared_model_va = "/var/www/myWizard.IngrAInAIServices.WebAPI.Python/intent_entity"
            shared_model_path = shared_model_va#/var/www/myWizard.IngrAInAIServices.WebAPI.Python/intent_entity

        else:
            shared_model_path = shared_model
        excel_data_dir = shared_model_path+"/"+folder+"/"+client_id+"/"+dc_id+"/"+correlation_id+"/excel_data/"
        #print("excel_data_dir is--------",excel_data_dir)

        if not os.path.exists(excel_data_dir):
            os.makedirs(excel_data_dir)
        
        #print("access token is--------",customurl_token)
        #Different methods for different data source
        #print("check data source................ ")
        if(data_source=="File"):
            #print("Same flow")
            file = request.files['file']
            #print("Checking file details")
            fileName = file.filename
            #print("Saving file")
            file.save(excel_data_dir + fileName)
            data_t=utils.read_excel(excel_data_dir + fileName)
        elif(data_source=="Custom"):
            data_details=payload_json['DataSourceDetails']
            #data_details={"HttpMethod" : "POST", "AppUrl" : "https://mywizardingrainapi-fusion-devtest-lx.aiam-dh.com/api/GetAIServiceIngestedData", "InputParameters" :  {"correlationid":"7af9b475-dbfb-45d8-b539-98a5f08f2348","noOfRecord":"40","DateColumn":""} }
            if auth_type == 'AzureAD':
                result=requests.post(data_details.get('AppUrl'),data=json.dumps(data_details.get('InputParameters')),headers={'Content-Type': 'application/json','Authorization': 'Bearer {}'.format(customurl_token)})
            
            elif auth_type == 'WindowsAuthProvider':
                result=requests.post(data_details.get('AppUrl'),data=json.dumps(data_details.get('InputParameters')),headers={'Content-Type': 'application/json'},auth=HttpNegotiateAuth())
            #print("post request result---------",result.json())
            data_array=result.json()
            df = pd.DataFrame(data_array)
            fileName="custom_data.xlsx"
            xlsx=excel_data_dir+fileName
            df.to_excel(xlsx)
            data_t=pd.read_excel(xlsx)


        #file = request.files['file']
        
        #print('fileName',fileName)
        if len(fileName)<1:
            raise ValueError('file is not uploaded')
        
        
      
        nlu_data =  shared_model_path+"/"+folder+"/"+client_id+"/"+dc_id+"/"+correlation_id+"/data/"
        #old_nlu_data=os.listdir(nlu_data)
        output = shared_model_path+"/"+folder+"/"+client_id+"/"+dc_id+"/"+correlation_id+'/output'
        #print("output path is-------",output)
        if not os.path.exists(nlu_data):
            os.makedirs(nlu_data)
        if not os.path.exists(output):
            os.makedirs(output)
        
        ALLOWED_EXTENSIONS = set(['json','xlsx'])
        if('.' in fileName and fileName.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS):			
            if fileName.rsplit('.', 1)[1].lower() == 'xlsx':	
                xlsx_raw = excel_data_dir + fileName
                xlsx = excel_data_dir + fileName
                base = os.path.basename(xlsx_raw)
                filename,ext = os.path.splitext(base)
                
                #key = load_key()
                #decrypt(xlsx, key)
                ##print("Reading excel")
                
                #print("Read excel")
                #data_t = utils.read_excel(xlsx_raw)
                ##print(type(data_t))
                #fileName1 = fileName.rsplit('.', 1)[0] + "_tmp." + fileName.rsplit('.', 1)[1]
                xlsx = excel_data_dir + fileName
                data_t.to_excel(xlsx, index = False)
                loc = xlsx
                #print("type of loc",type(loc))
                wb = xlrd.open_workbook(loc)
                s1 = wb.sheet_by_index(0)
                s1.cell_value(0,0)
                if (s1.nrows <= 2):
                    response["is_success"] = False
                    response["message"] = "No records in training file,atleast two of different intent should be present."
                    return jsonify(response)
                else:
                    #print("line no 556")
                    df_extracted = pd.DataFrame(columns=['Text', 'Intent', 'Value', 'Entity'])
                    #print("line no 558")
                    try:
                        df_original_check = pd.read_excel(xlsx)
                        text=df_original_check['Text']
                        intent=df_original_check['Intent']
                        value=df_original_check['Value']
                        entity=df_original_check['Entity']
                    except Exception as e:
                        response["is_success"] = False
                        response["message"] = "Template Mismatch. Please upload the template data in proper format."
                        return jsonify(response)
                    flag_synonym = True
                    if flag_synonym == True:
                        df_original = pd.read_excel(xlsx)
                        #print("df_original")
                        ##print(df_original['Value'])
                        ##print(df_original['Text'])
                        df_synonyms = get_synonym(df_original)
                        #df_without_stopwords = remove_stopwords(df_original)
                        #df_synonyms = get_synonym(df_without_stopwords)
                        df_extracted = get_singular_plural(df_synonyms)
                        #extracted_df.to_excel(xlsx)
                    xls_json_obj = XLSXHelper()
                    #print("df_extracted")
                    #print(df_extracted)
                    flag,xls_to_json_function = xls_json_obj.xlsx_to_json_conversion(df_extracted)
                    flag = 0                   
                    if(flag==0):
                        with open(nlu_data + '/' + filename + '.json','w') as js:
                            js.write(xls_to_json_function)
                            filePath = nlu_data + '/' + filename + '.json'             
                if os.path.exists(xlsx):
                    os.remove(xlsx)
            else:
                filePath = nlu_data + fileName
                file.save(filePath)
                #key = load_key()
                #decrypt(filePath, key)
                flag=0
            if(flag==0):
                train_path = None
                fixed_model_name=None
                persist_nlu_training_data = True
               
                #print("Intent Model thread started")
                try:
                    args=config_path+" "+nlu_data+" "+output+" "+correlation_id
                    if platform.system() == 'Linux':
                        pyExepath = "python"																																
                    elif platform.system() == 'Windows':
                        pyExepath =mainDirectory+"/venv/Scripts/python.exe"
                    pyFileName=currentDirectory+"/src/taskfile.py"
                    cmd=pyExepath+" "+pyFileName+" "+args
                    if platform.system() == 'Linux':
                        subprocess.Popen(cmd,close_fds=True,shell=True)     
                    elif platform.system() == 'Windows':
                        cmd1 = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
                        out, err = cmd1.communicate()                    
                    
                except Exception as ex:
                    error_val=ex.args
                    #print ("Exception in subprocess - "+str(ex))
                response["message"] = 'Training is in Progress'
                response["is_success"] = True

                
        else:
            response["is_success"] = False
            response["message"] = "File format not supported"
        
    except Exception as ex:
        #print(str(ex))
        response["message"] = str(ex)
        response["result"] = ""
        response["is_success"] = False
        logger.error(str(ex))
    ##print('response',response)
    return jsonify(response)

    
@app.route('/storytrain', methods=['POST'])
def train_story():
    payload = request.form['payload']
    payload_json = json.loads(payload)
    folder = "generated"
    response ={}
    
    currentDirectory = str(Path(__file__).parent)
    
    config_path = currentDirectory+'/config.yml'
    
    try:
             
        try:
            
            if ('client_id'not  in payload_json or len(payload_json['client_id'])<2):
                #print('client id empt')
                raise ValueError('client_id is not provided')
            elif ('dc_id' not  in payload_json or len(payload_json['dc_id'])<2):
                raise ValueError('dc_id is not provided')
            
        except Exception as ex:
            response["message"] = str(ex)
            #print(str(ex))
            response["is_success"] = False
            return jsonify(response)
        client_id=payload_json['client_id']
        dc_id=payload_json['dc_id']
        domain_file = request.files['domain']
        stories_file = request.files['stories']
        intents_file = request.files['intents']
        domain_fileName = domain_file.filename
        stories_fileName = stories_file.filename
        intents_fileName = intents_file.filename
        #print('domain_fileName ',domain_fileName)
        if len(domain_fileName)<1:
            raise ValueError('domain_fileName file is not uploaded')
        if len(stories_fileName)<1:
            raise ValueError('stories_fileName file is not uploaded')
        if len(intents_fileName)<1:
            raise ValueError('intents_fileName file is not uploaded')
        
        domain_path = currentDirectory+"/"+folder+"/"+client_id+"/"+dc_id+"/domain/"
        training_files_path =  currentDirectory+"/"+folder+"/"+client_id+"/"+dc_id+"/data/"
        output_path = currentDirectory+"/"+folder+"/"+client_id+"/"+dc_id+'/output'
        
        if not os.path.exists(domain_path):
            os.makedirs(domain_path)
        if not os.path.exists(training_files_path):
            os.makedirs(training_files_path)
        if not os.path.exists(output_path):
            os.makedirs(output_path)
        
        
        filePath = domain_path + "domain.yml"
        domain_file.save(filePath)
        filePath = training_files_path + "stories.md"
        stories_file.save(filePath)
        filePath = training_files_path + "nlu.md"
        intents_file.save(filePath)
        
        
        trained_model = train(domain_path,config_path,training_files_path,output_path,force_training=True,persist_nlu_training_data=True)
        file_basename = os.path.basename(trained_model)
        filename_without_extension = file_basename.split('.')[0]
        resp = unpack_model(trained_model,output_path+"/"+filename_without_extension)
        
        if (trained_model!=None):
            response["message"] = ""
            response["result"] = filename_without_extension
            response["is_success"] = True
        else:
            response["message"] = 'Unexpected Exception, Contact Administrator'
            response["result"] = ""
            response["is_success"] = False
        
    
    except Exception as ex:
        #print(str(ex))
        response["message"] = str(ex)
        response["result"] = ""
        response["is_success"] = False
    ##print('response',response)
    return jsonify(response)
    

if __name__ == '__main__':
    
    app.run()
   #train_nlp() 
   #parse_text()
    
