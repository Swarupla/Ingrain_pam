import sys
from keras import backend as k
from pathlib import Path
import numpy as np
import re
from io import StringIO
print ("starting next_word")
import platform
if platform.system() == 'Linux':
    from keras import backend as k
    from keras.preprocessing.sequence import pad_sequences
elif platform.system() == 'Windows':
    import tensorflow as tf
    #from keras import backend as k
    from tensorflow.keras.preprocessing.sequence import pad_sequences
import heapq
from ModelSaver import create_model,load_trained_model

mainDirectory = str(Path(__file__).parent.parent.parent.absolute())
print("Main directory path - "+mainDirectory)
sys.path.insert(0, mainDirectory)
import utils

# default range
nw_default = 1

# func to preprocess text
def preprocess_txt(text):
    new_text = re.sub('[^a-zA-Z0-9\n\.]', ' ', text)
    return new_text

# func to convert dataframe to txt	
def df_to_txt_nwp(dframe):
    dframe = dframe.replace('\n',' ', regex=True)
    s = StringIO()
    np.savetxt(s, dframe, fmt='%s')
    string_tmp = s.getvalue()
    processed_text = preprocess_txt(string_tmp)
    return processed_text

def generate_seq(model_name, content, seed_text, n_words,correlationId,uniId, top_prob=5):
    model, tokenizer, max_length = load_trained_model(model_name, content,correlationId,uniId)
    if(model is not None and tokenizer is not None and max_length is not None):
        # generate a sequence from a language model
        result = []
        in_text = seed_text
        # generate a fixed number of words
        for i in range(top_prob):
            for _ in range(n_words):
                # encode the text as integer
                encoded = tokenizer.texts_to_sequences([in_text])[0]
                # pre-pad sequences to a fixed length
                #max_length = 6
                encoded = pad_sequences([encoded], maxlen=max_length, padding='pre')
                # predict probabilities for each word
                yhat = model.predict_classes(encoded, verbose=0)
                # predict probabilities
                yprob = model.predict_proba(encoded,verbose=0)
                yprob_lst = heapq.nlargest(top_prob, range(len(yprob[0])), yprob.take)
                # map predicted word index to word
                out_word = ''

                for word, index in tokenizer.word_index.items():
                    if index == yprob_lst[i]:
                        out_word = word
                        break
                # append to input
                in_text += ' ' + out_word
            result.append(in_text)
            in_text = seed_text
        res_list = []
        for i in range(0,5):
            return_val_item = {}
            return_val_item["prediction"] = result[i]
            res_list.append(return_val_item)     
    else:
        print("Model could not be loaded.")
    print(res_list)
    return res_list
	
# API for detecting tone from a text input
def train_nwp(correlationId,pageInfo,uniId):
    try:
        logger = utils.logger('Get', correlationId)
	
        # getting dataframe
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            dframe = utils.data_from_chunks_offline_utility(correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            dframe = utils.data_from_chunks(correlationId, "AIServiceIngestData")
 
        # getting model name
        model_name_db = utils.GetModelName(correlationId,uniId)
	
        # getting col names from db	
        _, cols = utils.getRequestParams(correlationId,pageInfo,uniId)
        cols.sort()
	
        # creating dict for training data with cols as keys	
        content_dict = dict()
        for col in cols:
            content_dict[col] = df_to_txt_nwp(dframe[col])
        print("content_dict",content_dict)
		
        # calling training func
        for col in cols:
            model_name = model_name_db + "_" + col + ".h5"
            create_model(model_name,content_dict[col],col,correlationId,uniId)
        return True

    except Exception as ex:
        utils.logger(logger, correlationId, 'ERROR', 'Trace')
        return False 

def PredictNextWord(correlationId,pageInfo,uniId,testdata):
    try:
        logger = utils.logger('Get', correlationId)
        testdata = testdata[0]
        
	# columns to be evaluated
        cols_evaluate = {}
	
	# default value of n_words
        n_words = nw_default
      
        for col in testdata:
            if col == 'NWords':
                n_words = testdata[col]
            else:
                cols_evaluate[col] = testdata[col]
        n_words = int(n_words)	
        # getting dataframe
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            dframe = utils.data_from_chunks_offline_utility(correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            dframe = utils.data_from_chunks(correlationId, "AIServiceIngestData")
       
	
        # getting col names from db	
        _, cols = utils.getRequestParams(correlationId,"",uniId)
        cols.sort()
	
        # creating dict for training data with cols as keys		
        content_dict = dict()
        for col in cols:
            content_dict[col] = df_to_txt_nwp(dframe[col])
        
 
        # getting model name
        model_name_db = utils.GetModelName(correlationId,uniId)
        #newly added
        if len(cols_evaluate)>=1:
            status=False
            for key in cols_evaluate:
                data=cols_evaluate[key]
                pattern=re.search(r'^\s?$',data)
                if pattern:
                    break
                else:
                    status=True
            if not status:
                return False
        else:
            return False
        #newly added
        res_list = []		
        # calling evaluation func
        for col in cols_evaluate:
            model_name = model_name_db + "_" + col + ".h5"
            res_list = generate_seq(model_name,content_dict[col],cols_evaluate[col], n_words,correlationId,uniId,top_prob=5)
        k.clear_session()
        mydata=[]
        for data in res_list:
            val=data['prediction']
            val=str(val).encode('ascii',"ignore").decode('ascii')
            mydict={}
            mydict['prediction']=val
            mydata.append(mydict)
        res_list=mydata
    except Exception as e:
        utils.logger(logger, correlationId, 'ERROR', 'Trace')
        return False
    return res_list