#Created by Joydeep Sarkar
#Created on 14-Dec-2018
#Copyright MyWizard VA
import xlrd
import pandas as pd
import json
import math
from pathlib import Path
#currentDirectory = str(Path(__file__).parent)
#from shared.logger_helper import GenerateLogger
#from logger_helper import GenerateLogger

#log_generator = GenerateLogger()
#logger = log_generator.generate_logger(currentDirectory + "/xlsx_app.log")

from shared.logger_helper import GenerateLogger
log_generator = GenerateLogger()
currentDirectory = str(Path(__file__).parent.parent.parent.absolute())
logger = log_generator.generate_logger(currentDirectory + "/xlsx_validation.log")

# try:
    # from exceltest.shared.app_config import environment
# except Exception as e:
    # from shared.app_config import environment
# if(environment == "local"):
    # from shared.logger_helper import GenerateLogger
    # log_generator = GenerateLogger()
    # logger = log_generator.generate_logger('app_nlp.log')
# else:
    # from exceltest.shared.logger_helper import GenerateLogger
    # log_generator = GenerateLogger()
    # logger = log_generator.generate_logger('/var/www/ai/AI_NLP/app_nlp.log')

class XLSXHelper:
    def __init__(self):
        pass

    def xlsx_to_json_conversion(self, df_extracted):
        try:
            data_json = {
              "rasa_nlu_data": {
                "regex_features": [],
                "entity_synonyms": [],
                "common_examples": []
              }
            }
            #train = pd.read_excel(xls,index=False)
            train = df_extracted
            #train = xls
            comm_exp = []
            flag=0
            a=0
            train["Value"] = train["Value"].fillna("")
            train["Entity"] = train["Entity"].fillna("")
            for col,row in train.iterrows():
                d={}
                d["entities"] = []
                print(row)
                try:
                    print("inside try")

                    print(type(row["Intent"]))

                    #if not(row["Text"]==NaN or row["Intent"]==NaN):
                    if not((type(row["Text"]) is not str) or (type(row["Intent"]) is not str)):
                        
                        d["text"]=row["Text"]
                        d['intent']=row["Intent"]
                        print("inside if1")
                        #print(math.isnan(row["Value"]))
                        #if (row["Value"] != nan):
                        #if (math.isnan(row["Value"]) == False):
                        #if (not(type(row["Value"]) is str)):
                            #row["Value"] = str(row["Value"])     
                        if (type(row["Value"]) is not str):                     
                            print(type(row["Value"]))                 
                            row["Value"] = str(row["Value"])
                        #if (type(row["Value"]) is str):
                        if not(row["Value"] in (None, "","nan","nans")):
                            print("True")
                            #row["Value"] = str(row["Value"])
                            print("*******rowval******",row["Value"],type(row["Value"]))
                            ent_val = row["Value"].split(',')
                            ent_entity = row["Entity"].split(',')
                            if(len(ent_val) == len(ent_entity)):
                                print(len(ent_val))
                                entity_list = []
                            #print(type(ent_val))
                            #print(ent_val)
                                for i in range(len(ent_val)):
                                    ent = {}
                                    ent["start"] = row["Text"].find(ent_val[i])
                                    ent["end"] = row["Text"].find(ent_val[i]) + len(ent_val[i])
                                    ent["value"] = ent_val[i]
                                    ent["entity"] = ent_entity[i]
                                    entity_list.append(ent)
                                d["entities"] = entity_list
                            else:
                                flag=3
                                break

                        else:
                            d["entities"] = []
                        comm_exp.append(d)
                    else:

                        #if(row["Text"]==''):
                        if(type(row["Text"]) is not str):

                            flag=1
                            break
                        #if(row["Intent"]==''):
                        if(type(row["Intent"]) is not str):

                            flag=2
                            break
                except Exception as e:
                    logger.error(str(e))
                    print(e)  
            data_json['rasa_nlu_data']['common_examples']=comm_exp
            train_data_json = json.dumps(data_json)
            return flag,train_data_json 
        except Exception as e:
            print(e)
            logger.error(str(e))

if __name__== "__main__":
    xlsx = "/var/www/myWizard.IngrAInAIServices.WebAPI.Python/Intent_Entity/src/shared/template.xlsx" 
    xls_json_obj = XLSXHelper()
    flag,a = xls_json_obj.xlsx_to_json_conversion(xlsx)
    print("flag",flag)

