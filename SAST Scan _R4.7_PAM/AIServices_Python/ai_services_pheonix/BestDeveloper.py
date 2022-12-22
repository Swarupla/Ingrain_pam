# -*- coding: utf-8 -*-
"""
Created on Thu Jul 30 20:45:34 2020

@author: s.siddappa.dinnimani
"""

import warnings
warnings.filterwarnings("ignore")
import pandas as pd
import numpy as np
# Laoding libraries - ML related
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.naive_bayes import MultinomialNB
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.model_selection import GridSearchCV
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report
from sklearn.pipeline import Pipeline, make_pipeline
# Laoding libraries -Text Processing
from nltk.tag import pos_tag
from nltk.stem import PorterStemmer, WordNetLemmatizer
import nltk
import re
from nltk.corpus import wordnet as wn
from nltk.corpus import stopwords
import string
# Laoding libraries -Utility
from functools import lru_cache
import joblib
#from sklearn.externals import joblib
from scipy.sparse import hstack  
from sklearn.preprocessing import LabelEncoder,StandardScaler
from scipy.sparse import csr_matrix
from sklearn.metrics import confusion_matrix,accuracy_score

import os,sys,inspect
currentdir = os.path.dirname(os.path.abspath(inspect.getfile(inspect.currentframe())))
parentdir = os.path.dirname(currentdir)
sys.path.insert(0,parentdir) 

import utils

# =============================================================================
# Text Processing
# =============================================================================
#Defining lematizer, pos tagger and stemmer
wnl     = WordNetLemmatizer()
tagger  = pos_tag
porter  = PorterStemmer()

#memonization - to increase speed n efficiency
lemmatize_mem = lru_cache(maxsize=16384)(wnl.lemmatize)
tagger_mem = lru_cache(maxsize=16384)(tagger)

# ***Defining Fucntions***
#convert treebank tags to wordnet tags
def tree_2_word(treebank_tag):
    if treebank_tag.startswith('J'):
        return wn.ADJ
    elif treebank_tag.startswith('V'):
        return wn.VERB
    elif treebank_tag.startswith('R'):
        return wn.ADV
    else:
        return wn.NOUN
    
def lemmatize(word,pos=wn.NOUN):
    return(lemmatize_mem(word,pos))

     
def text_process(t,stem = True,lemmetize= True,pos = True):
    stops = set(stopwords.words("english"))               # Defining stop words
    t = re.sub('[^a-zA-Z]',' ',t)
    t = re.sub('[\n\t\r]+',' ',t)                         # Remove linebreak, tab, return
    t = t.lower()                                         # Convert to lower case
                             # Remove Non-letters
    sentence = nltk.sent_tokenize(t)                      # Sentence wise tokenize
    modified_sentence=""
    for s in sentence:
        words = nltk.word_tokenize(s)                     # Word Tokenization 
        if pos:                                           # Part of speech Tagging
            tag_words = pos_tag(words)                        
            lemmatized= " "
            for tw in tag_words:
                if tw[0] not in stops:
                    if lemmetize:                         # lemmatization
                        lemma=lemmatize(tw[0],tree_2_word(tw[1])) 
                    elif stem:
                        lemma = porter.stem(tw[0])    # Stemming
                    else:
                        lemma = tw[0]
                 
                    lemmatized=lemmatized+" "+lemma
         
        modified_sentence=modified_sentence+" "+lemmatized
    t = re.sub('['+string.punctuation+']+','',               modified_sentence)                          # Remove Punctuations     
    t = re.sub('\s+\s+',' ',t)                             # Remove double whitespace
    return(t)
    
def splitDataFrameList(df,target_column,separator):
    ''' df = dataframe to split,
    target_column = the column containing the values to split
    separator = the symbol used to perform the split

    returns: a dataframe with each entry for the target column separated, with each element moved into a new row. 
    The values in the other columns are duplicated across the newly divided rows.
    '''
    def splitListToRows(row,row_accumulator,target_column,separator):
        split_row = row[target_column].split(separator)
        for s in split_row:
            new_row = row.to_dict()
            new_row[target_column] = s
            row_accumulator.append(new_row)
    new_rows = []
    df.apply(splitListToRows,axis=1,args = (new_rows,target_column,separator))
    new_df = pd.DataFrame(new_rows)
    return new_df   
    
def model_training(data,correlationId,UniId):
    data.Description=data.Description.astype(str)
    data.Description=data.Description.apply(text_process)
    data1=data.copy()
    data1.dropna(subset=["filesmodified","createdatsourcebyuser","blockerissuecount"],inplace=True)
    data1.reset_index(drop=True,inplace=True)
    count_vect = CountVectorizer(stop_words="english")
    X_train_counts = count_vect.fit_transform(data1['Description'])
   
#    with open('count_vect.pkl', 'wb') as f:
#        pkl.dump(count_vect, f)
    utils.save_model_for_BestDeveloper(correlationId,"count_vect",UniId,count_vect)
    clf = MultinomialNB().fit(X_train_counts,data1['filesmodified'])
#    with open('clf.pkl', 'wb') as c:
#        pkl.dump(clf, c)
    utils.save_model_for_BestDeveloper(correlationId,"Model",UniId,clf)
    data2 = data1[["createdatsourcebyuser","filesmodified","blockerissuecount"]].copy()
    data2["filesmodified"] = data2[["filesmodified"]].fillna('')
    data2["filesmodified"] = data2["filesmodified"].str.strip()
    jen_df =splitDataFrameList(data2,"filesmodified",',')
    jen_df["filesmodified"] = jen_df["filesmodified"].str.strip()
    jen_df['blockerissuecount'] = jen_df['blockerissuecount'].astype('float64')
    temp_df = jen_df[["createdatsourcebyuser","filesmodified","blockerissuecount"]].groupby(["createdatsourcebyuser","filesmodified"]).sum().reset_index()
    temp_df['blockerissuecount'] = temp_df['blockerissuecount'].astype('float64')
    temp_df = temp_df.rename(columns={"blockerissuecount":"blockerissuecount_total"})
    jen_df = jen_df.merge(temp_df,how='inner',left_on=["createdatsourcebyuser","filesmodified"],
                                              right_on=["createdatsourcebyuser","filesmodified"])
    jen_df = jen_df[["createdatsourcebyuser","filesmodified","blockerissuecount_total"]].drop_duplicates()

    module_df = jen_df[["filesmodified","blockerissuecount_total"]].groupby(["filesmodified"]).sum().reset_index()
    module_df = module_df.rename(columns={"blockerissuecount_total" : "all_blockerissuecount_total"})
    
    
    jen_df = jen_df.merge(module_df, how='left',left_on=["filesmodified"],right_on=["filesmodified"])
    jen_df["blockerissuecount_total_scale"] = jen_df["blockerissuecount_total"] / module_df["all_blockerissuecount_total"]
    jen_df = jen_df.fillna(0)

    jen_df["blockerissuecount_score"] = (1.0 - (1.0 - jen_df["blockerissuecount_total_scale"])) * (1.0 - (1.0 - jen_df["blockerissuecount_total_scale"]))
    jen_df["final_score"]=jen_df["blockerissuecount_score"]
    utils.save_model_for_BestDeveloper(correlationId,"Scores",UniId,jen_df)
#    with open('jen_df.pkl', 'wb') as g:
#        pkl.dump(jen_df, g)
        
    return "Model Traning completed"
	


def BestDeveloperPrediction(test_data,correlationId,UniId):
    """
   For each row of test data we will run a for loop for each file which has the maximum probability.
   for each file we are finding the Best Developer and appending to the output.This for loop will
   recursively run untill all the developers are added in order of increasing blocker violation score
    """
    
    test_data.Description=test_data.Description.astype(str)
    test_data.Description=test_data.Description.apply(text_process)
    count_vect = utils.load_model(correlationId,"count_vect",UniId)
#    with open('count_vect.pkl ', 'rb') as f:
#        count_vect =pkl.load(f)
#    with open('clf.pkl', 'rb') as c:
#        clf=pkl.load(c)
    clf = utils.load_model(correlationId,"Model",UniId)
#    with open('jen_df.pkl', 'rb') as g:
#        jen_df=pkl.load(g)
    jen_df = utils.load_model(correlationId,"Scores",UniId)
    X_new_counts = count_vect.transform(test_data.Description)


  
    pred_dict = {}
    for i in range(0,len(clf.classes_)):
        pred_dict[clf.classes_[i]]=[]



    #if X_new_counts.nnz > 0 :
    for j in range(X_new_counts.shape[0]):
        predicted = clf.predict(X_new_counts[j])
        pred = clf.predict_proba(X_new_counts[j])
        #print(pred)
        for i in range(0,len(clf.classes_)):
            pred_dict[clf.classes_[i]].append(pred[0][i])
    df=pd.DataFrame(pred_dict)
    df1=df.T
    df1.reset_index(inplace=True)
    df_pred =splitDataFrameList(df1,"index",',')
    df_pred=df_pred.T
    df_pred.rename(columns=df_pred.iloc[0],inplace=True)
    df_pred=df_pred.iloc[1:]
    df_pred_dict=dict(df_pred)
    
    
    line_items=[]
    return_msg=''
    files_max_prob=list(df_pred.max(axis=1))
    
        
    for i,wid  in enumerate(test_data["workitemexternalid"]):
        all_files=set()
        dev_ids=[]
        dev_scores=[]
        flag=1
        while flag==1:
            for file in df_pred_dict.keys():


                if df_pred_dict[file][i]==files_max_prob[i]:
                    all_files.add(file)



                    #return_msg = return_msg + " " + pred_dict[file][i] + "(" + str(round(pred_dict[file][i]*100,2)) + "%)" 
                    df_dev_train_pred = jen_df[jen_df["filesmodified"] == file]
                    df_dev_train_pred=df_dev_train_pred[~df_dev_train_pred["createdatsourcebyuser"].isin(dev_ids)]
                    df_dev_train_pred.reset_index(inplace=True,drop=True)
                    if (df_dev_train_pred.shape[0] != 0):
                        best_dev_df = df_dev_train_pred.iloc[df_dev_train_pred["final_score"].idxmin()]
                        dev_ids.append(best_dev_df["createdatsourcebyuser"])
                        dev_scores.append(best_dev_df["final_score"])





            
            temp=[dev for dev in df_dev_train_pred["createdatsourcebyuser"]]
            if len(temp)==0:
                flag=0


        json_element = {
                        #'Predicted Module' : pred_module,
                        #'TestIndex' :  i,
                        'workitemexternalid':wid,
                        'predictedFile' : list(all_files),
                        'Probability' : str(round(files_max_prob[i]*100,2)) + "%",
                        'BestDeveloper' : dev_ids,
                        
                        }

        if len(json_element ['BestDeveloper']) > 5:
            json_element ['BestDeveloper'] = json_element ['BestDeveloper'][0:5]


        line_items.append(json_element)
        


    
    return line_items