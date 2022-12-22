# -*- coding: utf-8 -*-
"""
Created on Tue Dec 24 11:36:09 2019

@author: saurav.b.mondal
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
import numpy as np
import json
from SSAIutils import utils
import sys
import base64
import json
from SSAIutils import EncryptData
import uuid
import re
from datetime import datetime
from pandas import Timestamp
def evaluate_condition(df,key,pageInfo="DataPreprocessing",constant_val_list=[]):
    values = []
    flag_column = False
    flag_value = False
    constant_val = None
    
    if 'OperationType' in list(key.keys()):
        if str(key['OperationType']).lower() == 'mean':
            if pageInfo!="publishModel":
                values.append(str(df[key['ColDrp']].mean()))
            else:
                values.append(str(constant_val_list[0]))
                constant_val_list.remove(constant_val_list[0])
        elif str(key['OperationType']).lower() == 'median':
            if pageInfo!="publishModel":
                values.append(str(df[key['ColDrp']].median()))
            else:
                values.append(str(constant_val_list[0]))
                constant_val_list.remove(constant_val_list[0])
        elif str(key['OperationType']).lower() == 'mode':
            if pageInfo!="publishModel":
                values.append(str((df[key['ColDrp']].mode())[0]))
            else:
                values.append(str(constant_val_list[0]))
                constant_val_list.remove(constant_val_list[0])
        elif str(key['OperationType']).lower() == 'sd':
            if pageInfo!="publishModel":
                values.append(str(df[key['ColDrp']].std()))
            else:
                values.append(str(constant_val_list[0]))
                constant_val_list.remove(constant_val_list[0])
        elif str(key['OperationType']).lower() == 'max':
            if pageInfo!="publishModel":
                values.append(str(df[key['ColDrp']].max()))
            else:
                values.append(str(constant_val_list[0]))
                constant_val_list.remove(constant_val_list[0])
        elif str(key['OperationType']).lower() == 'min':
            if pageInfo!="publishModel":
                values.append(str(df[key['ColDrp']].min()))
            else:
                values.append(str(constant_val_list[0]))
                constant_val_list.remove(constant_val_list[0])
        elif str(key['OperationType']).lower() == 'custom' or str(key['OperationType']).lower() == 'values':
            if pageInfo!="publishModel":
                values.append(str(key['Value']))
            else:
                values.append(str(constant_val_list[0]))
                constant_val_list.remove(constant_val_list[0])
        elif str(key['OperationType']).lower() == 'date & time':
            '''
            for date columns: convert using the following code:
            df['date'] = pd.to_datetime(df['date'])
            '''
            if str(key['Column']).lower() == 'day':
                values.append(str('df["'+key['ColDrp']+'"].dt.day'))
            elif str(key['Column']).lower() == 'month':
                values.append(str('df["'+key['ColDrp']+'"].dt.month'))
            elif str(key['Column']).lower() == 'year':
                values.append(str('df["'+key['ColDrp']+'"].dt.year'))
            elif str(key['Column']).lower() == 'hour':
                values.append(str('df["'+key['ColDrp']+'"].dt.hour'))
            elif str(key['Column']).lower() == 'minute':
                values.append(str('df["'+key['ColDrp']+'"].dt.minute'))
            elif str(key['Column']).lower() == 'second':
                values.append(str('df["'+key['ColDrp']+'"].dt.second'))
            elif str(key['Column']).lower() == 'weekday':
                values.append(str('df["'+key['ColDrp']+'"].dt.dayofweek'))
            if not flag_column:
                flag_column = True
        elif str(key['OperationType']).lower() in ['sqrt','cbrt','sqr','cube','original','percentage']:
            if str(key['OperationType']).lower() == 'sqrt':
                values.append(str('df["'+key['ColDrp']+'"]**(1/2)'))
            if str(key['OperationType']).lower() == 'cbrt':
                values.append(str('df["'+key['ColDrp']+'"]**(1/3)'))
            elif str(key['OperationType']).lower() == 'sqr':
                values.append(str('df["'+key['ColDrp']+'"]**(2)'))
            elif str(key['OperationType']).lower() == 'cube':
                values.append(str('df["'+key['ColDrp']+'"]**(3)'))
            elif str(key['OperationType']).lower() == 'original':
                values.append(str('df["'+key['ColDrp']+'"]'))
            elif str(key['OperationType']).lower() == 'percentage':
                values.append('.'+str(key['Value'])+'*'+str('df["'+key['ColDrp']+'"]'))
            if not flag_column:
                flag_column = True
        elif str(key['OperationType']).lower() in ['contains','matches','substring','index','replace','startswith','endswith','length','regexsearch']:
            if str(key['OperationType']).lower() == 'contains':
                values.append(str('df["'+key['ColDrp']+'"].str.lower().str.contains("'+key['Value']+'".lower())'))
            elif str(key['OperationType']).lower() == 'matches':
                values.append(str('pd.Series([True if str(element).lower()  == "'+key['Value']+'".lower() else False for element in df["'+key['ColDrp']+'"]])'))
            elif str(key['OperationType']).lower() == 'substring':
                values.append(str('df["'+key['ColDrp']+'"].str.lower().str.slice('+str(key['Value'])+','+str(key['Value2'])+')'))
            elif str(key['OperationType']).lower() == 'index':
                values.append(str('df["'+key['ColDrp']+'"].str.lower().str.find("'+key['Value']+'".lower())'))
            elif str(key['OperationType']).lower() == 'replace':
                values.append(str('df["'+key['ColDrp']+'"].str.replace("'+key['Value']+'","'+key['Value2']+'",flags=re.IGNORECASE, regex=True)'))
            elif str(key['OperationType']).lower() == 'startswith':
                values.append(str('df["'+key['ColDrp']+'"].str.lower().str.startswith("'+key['Value']+'".lower())'))
            elif str(key['OperationType']).lower() == 'endswith':
                values.append(str('df["'+key['ColDrp']+'"].str.lower().str.endswith("'+key['Value']+'".lower())'))
            elif str(key['OperationType']).lower() == 'length':
                values.append(str('df["'+key['ColDrp']+'"].str.lower().str.len()'))
            #elif str(key['OperationType']).lower() == 'regexsearch':
            #    values.append(str('df["'+key['ColDrp']+'"].str.lower().str.contains("'+key['Value']+'", na=False, regex=True)'))
            if not flag_column:
                flag_column = True
        flag_value = True
    #print("flag_column:",flag_column)
    #print(values)
    if flag_value:
        if flag_column:
            col= eval(values[0])
            col.reset_index(inplace=True, drop=True)
        else:
            try:
                col = pd.Series(eval(values[0]),index=list(range(df.shape[0])))
                col.reset_index(inplace=True, drop=True)
                if pageInfo!="publishModel":
                    constant_val = eval(values[0])
            except Exception:
                col = pd.Series(values[0],index=list(range(df.shape[0])))
                col.reset_index(inplace=True, drop=True)
                if pageInfo!="publishModel":
                    constant_val = values[0]
        if 'Operator' in list(key.keys()):
            op_value = key['Operator']
        else:
            op_value = ''
        if pageInfo!="publishModel":
            return col, op_value,constant_val 
        else:
            return col, op_value,constant_val_list
    else:
        if 'Operator' in list(key.keys()):
            op_value = key['Operator']
        else:
            op_value = ''
        if pageInfo!="publishModel":
            return '', op_value,constant_val
        else:
            return '', op_value,constant_val_list

#evaluate_condition(df, {'OperationType': 'Max', 'ColDrp': 'Bedroom2'})
#evaluate_condition(df, {'Operator': 'Add', 'OperationType': 'Median', 'ColDrp': 'Rooms'})
def add_new_features(df,mappings,pageInfo="DataPreprocessing",value_Store = None):
    #mappings = json.loads(feature_mapping)
    feature_not_created = {}
    if pageInfo!="publishModel":
        value_Store={}
    for feature in mappings.keys():
        #print(feature)
        new_column_list = {}
        new_column_list1 = {}
        counter = 0
        counter1 = 0
        string_final = ''
        string_final1 = ''
        item = mappings[feature]
        item =sorted(item.items(), key = lambda kv:(kv[0], kv[1]))
        item = dict(item)
        if pageInfo!="publishModel":
            constant_val_list = []
        else:
            constant_val_list = value_Store[feature]
        try:
            for key in item:
                if key not in ['value_check','value']:
                    #print(key,":" ,item[key])
                    if pageInfo!="publishModel":
                        col,op_value,constant_val = evaluate_condition(df,item[key])
                    
                        if constant_val is not None:
                            constant_val_list.append(constant_val)
                    else:
                        col,op_value,constant_val_list = evaluate_condition(df,item[key],pageInfo="publishModel",constant_val_list = constant_val_list)
                    
                    if op_value != '':
                        
                        if str(op_value).lower() in ['greater than','less than', 'greater than equal to','less than equal to','equal to','not equal to']:
                            if str(op_value).lower() == 'greater than':
                                string_final = string_final +'>'+' '
                            elif str(op_value).lower() == 'less than':
                                string_final = string_final +'<'+' '
                            elif str(op_value).lower() == 'greater than equal to':
                                string_final = string_final +'>='+' '
                            elif str(op_value).lower() == 'less than equal to':
                                string_final = string_final +'<='+' '
                            elif str(op_value).lower() == 'equal to':
                                string_final = string_final +'=='+' '
                            elif str(op_value).lower() == 'not equal to':
                                string_final = string_final +'!='+' '
                        elif str(op_value).lower() in ['add','subtract','multiply','divide', 'days between','months between','years between','hours between','minutes between','seconds between']:
                            if str(op_value).lower() == 'add':
                                string_final = string_final +'+'+' '
                            elif str(op_value).lower() in ['subtract','days between','months between','years between','hours between','minutes between','seconds between' ]:
                                string_final = string_final +'-'+' '
                            elif str(op_value).lower() == 'multiply':
                                string_final = string_final +'*'+' '
                            elif str(op_value).lower() == 'divide':
                                string_final = string_final +'/'+' '
                    if isinstance(col, pd.Series):
                        new_column_list[counter] = col
                        string_final = string_final+'new_column_list['+str(counter)+'] '
                        counter+=1
                    
                    if str(op_value).lower() in ['days between','months between','years between','hours between','minutes between','seconds between' ]:
                        if str(op_value).lower() == 'days between' :
                            str_to_replace = 'D'
                        if str(op_value).lower() == 'months between':
                            str_to_replace = 'M'
                        if str(op_value).lower() == 'years between':
                            str_to_replace = 'Y'
                        if str(op_value).lower() == 'hours between':
                            str_to_replace = 'h'
                        if str(op_value).lower() == 'minutes between':
                            str_to_replace = 'm'
                        if str(op_value).lower() == 'seconds between':
                            str_to_replace = 's'
                        string_final = "("+string_final+")/np.timedelta64(1,'{}')".format(str_to_replace)
                    if str(op_value).lower() == 'business days between':
                            string_final = string_final.replace('new_column_list['+str(counter-2)+'] ',"")
                            string_final = string_final.replace('new_column_list['+str(counter-1)+'] ',"")
                            #string_final = string_final+"pd.Series([len(pd.bdate_range(x,y))for x,y in zip("+'new_column_list['+str(counter-2)+"] "+","+'new_column_list['+str(counter-1)+"])]) "
                            string_final = string_final+"pd.Series([np.busday_count(x, y) for x,y in zip([d.date() for d in "+'new_column_list['+str(counter-2)+"]] "+", [d.date() for d in "+'new_column_list['+str(counter-1)+"]])]) "
                            #print(string_final)
                    if str(op_value).lower() in ['and', 'or', 'not']:
                            try:
                                #print("string_final:", string_final)
                                new_column_list1[counter1] = eval(string_final)
                            except Exception as e :
                                feature_not_created[feature]=str(feature)+": Feature not created due to wrong combination of OperationType and Column"
                                break
                            string_final = ''
                            #counter = 0
                            #new_column_list = {}
                            string_final1 = string_final1+'new_column_list1['+str(counter1)+'] '
                            if str(op_value).lower() == 'and':
                                string_final1 = string_final1 + '&'+ ' '
                            if str(op_value).lower() == 'or':
                                string_final1 = string_final1 + '|'+ ' '
                            if str(op_value).lower() == 'not':
                                string_final1 = string_final1 + '== ~'+ ' '
                            counter1+=1
            try:
                new_column_list1[counter1] = eval(string_final)
            except TypeError as e :
                if e.args[0] == "DatetimeArray subtraction must have the same timezones or no timezones":
                    raise Exception("Time Zones of the selected date columns do not match")
                else:
                    raise Exception("Feature not created due to wrong combination of OperationType and Column")
            except Exception as e :
                raise Exception("Feature not created due to wrong combination of OperationType and Column")
            string_final1 = string_final1+'new_column_list1['+str(counter1)+'] '
        except Exception as e:
            feature_not_created[feature]=e.args[0]
            continue
        #print("string_final1:", string_final1)
        #print(eval(string_final))
        try:
            final_df_column = eval(string_final1)
        except Exception as e :
            feature_not_created[feature]=str(feature)+": Feature not created due to wrong combination of OperationType and Column"
            continue
        #print(final_df_column)
        #print(type(final_df_column))
        if final_df_column.dtype == 'float64':
            final_df_column=final_df_column.round(2)
        if final_df_column.nunique()==1 and pageInfo!="publishModel" :
            feature_not_created[feature]=str(feature)+": Feature not created as the final feature vector contains only one value"
         
        elif (final_df_column.isnull().sum()/len(final_df_column))>20:
            feature_not_created[feature]=str(feature)+": Feature not created as the final feature vector has more than 20% missing values"
          
        elif 'value_check' in list(item.keys()) and item['value_check'] == "true":
            
            df[feature] = np.nan
            df_true,_ ,_ = evaluate_condition(df,item['value']['true'])
            df_false,_ ,_= evaluate_condition(df,item['value']['false'])
            for item in range(0, final_df_column.__len__()):
                if final_df_column[item]:
                    
                    df[feature].iloc[[item]] = df_true.iloc[item]
                else:
                    df[feature].iloc[[item]] = df_false.iloc[item]
        else:
            df[feature] = final_df_column
        if pageInfo!="publishModel":
            value_Store[feature] = constant_val_list
    if pageInfo!="publishModel":
        return df,feature_not_created,value_Store
    else:
        return df,feature_not_created
    

def main(invokeAddFeature,correlationId,pageInfo, userId):
    try:
        logger = utils.logger('Get',correlationId)
        timeSeries = False
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()
        if data_json[0]['ProblemType']!="TimeSeries":
            input_columns = data_json[0].get('InputColumns')
        else:
            timeSeries = True
            input_columns = [data_json[0]['TimeSeries']['TimeSeriesColumn']]
        target_variable = data_json[0].get('TargetColumn')
        uniqueIdentifier = data_json[0].get('TargetUniqueIdentifier')
        
        # Fetch data
        if uniqueIdentifier!=None:
            data_cols_t = input_columns + [target_variable]+[uniqueIdentifier]
        else:
            data_cols_t = input_columns + [target_variable]
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            data = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            data = utils.data_from_chunks(corid=correlationId,collection="PS_IngestedData")																     
        try:
            data = data[data_cols_t]
        except Exception:
            data_cols_t=  list(set(input_columns)-set([target_variable]))
            data = data[data_cols_t]
        data_cols = data.columns        
        data_dtypes = dict(data.dtypes)
        ndata_cols = data.shape[1]
        datasize = (sys.getsizeof(data)/ 1024) / 1024               
        #print (data_cols)
        #Fetch user selected data types
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        Features = features_data_json[0].get('Feature Name')
        if EnDeRequired :
            t = base64.b64decode(Features) #DE55......................................
            Features = json.loads(EncryptData.DescryptIt(t))
        #print (Features.keys(),data_cols_t,data.columns)
        #Removing Correlated columns
        try:
            datacols_F=list(Features.keys())
            data = data[datacols_F]
            data_cols = data.columns        
            data_dtypes = dict(data.dtypes)
            #print(data_dtypes)
            ndata_cols = data.shape[1]
            datasize = (sys.getsizeof(data)/ 1024) / 1024 
        except:
            utils.logger(logger,correlationId,'INFO',"Features dictionary in Add Features")
        #print(target_variable,data.columns)
        #print("inside main")
        # Fetch the changed data type
        features_dtypesf = {}
        for key,value in Features.items():        
            d_type = [key1 for key1,value1 in value.get('Datatype').items() if value1 =='True']
            if d_type:    
                features_dtypesf.update({key:d_type[0]})
            else:
                features_dtypesf.update({key:'ND'})
        #Throw category type not assigned if ND,
        #print(data_dtypes)
        #print(features_dtypesf)
        '''
        category_cols=[]
        for x, y in features_dtypesf.items():
            if y=='category':
                category_cols.append(x)
        
       
        for c in category_cols:
             #data[c] = data[c].replace(d1,'_',regex=True)
             data[c] = data[c].replace('[^a-zA-Z0-9 ]', '_', regex = True) '''
        
        # Change the data type
        cate = ['category']
        inte = ['float64', 'int64']
        try:
            for key4,value4 in features_dtypesf.items():        
                #print(key4,value4)   
                if key4 in data_dtypes.keys():           
                    if (data_dtypes.get(key4)).name in ['float64','int64']:
                        if value4 in cate:
                           data[key4] = data[key4].astype(str)                      
                    elif (data_dtypes.get(key4)).name == 'object':  
                        if value4 == inte[1]:   
                           data[key4] = data[key4].astype(int)
                        elif value4 == inte[0]:
                           data[key4] = data[key4].astype('float64')
                        utils.logger(logger,correlationId,'INFO',"Features_dtypesf dictionary in Add Features")
                    elif (data_dtypes.get(key4)).name =='bool':
                        data[key4] = data[key4].astype(str)
                else:
                    print("Not there")
        except Exception:
            raise Exception("Datatype selection is not proper for column '{}'".format(key4))
               
        #print("insida Done")
        #print (features_dtypesf)
        # Remove Id and Text columns
        '''CHANGES STARTED'''
        #problemtypef=fetchProblemType(correlationId)
        drop_cols_t=[]   
        text_list = []
        date_cols=[]
        for key5,value5 in features_dtypesf.items():
            if not timeSeries:            
                if value5 in ['Id','Text','datetime64[ns]','datetime64[ns, UTC]']:
                    drop_cols_t.append(key5)
                if value5 == 'Text':
                    text_list.append(key5)
                if value5 in ['datetime64[ns]','datetime64[ns, UTC]']:
                    date_cols.append(key5)
            if timeSeries:
                if value5 in ['Id']:
                    drop_cols_t.append(key5)
            #if problemtypef=='Text_Classification':
            #    if value5 in ['Id','datetime64[ns]']:
            #        drop_cols_t.append(key5)
        drop_cols_t= list(set(drop_cols_t)- set(text_list)-set(date_cols))
        if not timeSeries:
            for cols_dates in date_cols:
                #data[cols_dates] =  pd.to_datetime(data[cols_dates], format='YYYY-MM-DD HH:MM:SS')
                #data[cols_dates] = data[cols_dates].dt.strftime("YYYY-MM-DD HH:MM:SS")
                data[cols_dates] = pd.to_datetime(data[cols_dates],dayfirst=True)
                data = data[data[cols_dates].notnull()]
                data.reset_index(inplace = True, drop=True)
                
            '''CHANGES ENDED'''
        map_encode_new_feature={}
        features_created = []
        feature_not_created ={}
        value_store= {}
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        try:
            feature_mapping = features_data_json[0].get('NewAddFeatures')
            #feature_mapping = invokeAddFeature['NewAddFeatures']
            if feature_mapping !={} and feature_mapping != None:
                data,feature_not_created,value_store = add_new_features(data,feature_mapping)
                if len(feature_mapping) == len(feature_not_created):
                            print("No Features were created")
                else:
                    features_created = list(set(list(feature_mapping.keys())) - set(feature_not_created.keys()))
                    #data.dropna(how='all', axis=1, inplace=True)
                    
                    for key in list(feature_mapping.keys()):
                        #print("key:", key)
                        #print(data[key])
                        if key in features_created:
                            if data.dtypes[key] not in ['int64','float64']:
                                map_encode_new_feature[key] = {'attribute': 'Nominal','encoding': 'Label Encoding','ChangeRequest': 'True','PChangeRequest': 'False'}
                                if data.dtypes[key] =='bool':
                                    data[key] = data[key].astype(str)
                existingFeatures = []
                if len(features_created)>0:
                    for newCol in feature_mapping:
                        if newCol in features_created:
                            for each in feature_mapping[newCol]:
                                try:
                                   #print(each)
                                   if each != 'value_check' and each != 'value':
                                       existingFeatures.append(feature_mapping[newCol][each]['ColDrp'])
                                except KeyError:
                                   utils.logger(logger,correlationId,'INFO',"Features mapping Add Features")
                existingFeatures = list(set(existingFeatures))
                dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
                Id = str(uuid.uuid4())
                dbcollection.update_many({"CorrelationId"     : correlationId},
                                    { "$set":{      
                                       "CorrelationId"     : correlationId,
                                       "pageInfo"          : pageInfo,
                                       "CreatedBy"         : userId,
                                       #"Data"              : data[features_created],
                                       "Feature_Not_Created" : feature_not_created,
                                       "Features_Created" : features_created,
                                       "Map_Encode_New_Feature":list(map_encode_new_feature.keys()),
                                       "Add_Feature_Value_Store":value_store,
                                       "Existing_Features":existingFeatures
                                       }},upsert=True)
                dbconn.close()
                filesize = utils.save_data(correlationId,pageInfo,userId,'DE_NewFeatureData',data=data,datapre=False)
            else:
                dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
                Id = str(uuid.uuid4())
                dbcollection.update_many({"CorrelationId"     : correlationId},
                                    { "$set":{      
                                       "CorrelationId"     : correlationId,
                                       "pageInfo"          : pageInfo,
                                       "CreatedBy"         : userId,
                                       #"Data"              : data[features_created],
                                       "Feature_Not_Created" : [],
                                       "Features_Created" : [],
                                       "Map_Encode_New_Feature":[],
                                       "Add_Feature_Value_Store":[],
                                       "Existing_Features":[]
                                       }},upsert=True)
                dbconn.close()
        except Exception as E:
            logger = utils.logger('Get',correlationId) 
        
            utils.logger(logger,correlationId,'ERROR','Trace',str(None))
            dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
            Id = str(uuid.uuid4())
            dbcollection.update_many({"CorrelationId"     : correlationId},
                                { "$set":{      
                                   "CorrelationId"     : correlationId,
                                   "pageInfo"          : pageInfo,
                                   "CreatedBy"         : userId,
                                   #"Data"              : data[features_created],
                                   "Feature_Not_Created" : [],
                                   "Features_Created" : [],
                                   "Map_Encode_New_Feature":[],
                                   "Add_Feature_Value_Store":[],
                                   "Existing_Features":[]
                                   }},upsert=True)
            dbconn.close()
    except Exception as e:
        print("exception")
        logger = utils.logger('Get',correlationId) 
        
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        raise Exception(e.args[0])
        