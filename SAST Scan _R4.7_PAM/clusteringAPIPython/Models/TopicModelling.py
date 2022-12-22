# -*- coding: utf-8 -*-
"""
Created on Wed Jul 29 14:51:57 2020

@author: harsh.nandedkar
"""
import pandas as pd
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
#from datapreprocessing import Textclassification
from nltk.corpus import stopwords

from sklearn.decomposition import NMF, LatentDirichletAllocation, TruncatedSVD
from sklearn.manifold import TSNE
from SSAIutils import utils



def preprocessing(series,language='english'):
    stop = stopwords.words(language)
    #series = series.apply(lambda word1: " ".join(word1.lower() for word1 in str(word1).split()))
    series = utils.lambda_execute(series)
    #series = series.apply(lambda word2: " ".join(word2 for word2 in str(word2).split() if str(word2) not in stop))
    series = utils.lambda_execute1(series,stop)
    series = series.str.replace(r'[^\w\s]','')
    series = series.str.replace(r'\d+', '')
    #series.apply(lambda x: str(TextBlob(x).correct()))
    return series

def flatten(empty_list):
    l=[]
    for i in empty_list:
        t=type(i)
        if t is tuple or t is list:
            for i1 in flatten(i[0:3]):
                print(i1)
                l.append(i1)
        else:
            l.append(i)
    return l

def list_chunks(lst,n):
    for i in range(0,len(lst),n):
        yield lst[i:i+n]
        
def only_unique_clusteruniquevalues(cluster_dict_keys,cluster_dict_values,modelname=None):
    empty_list=list(cluster_dict_values) 
    temp_list=[]       
    for x,y in enumerate(empty_list):
            temp_list.append(empty_list[x])
    
    dic={}
    for index,value in enumerate(temp_list):
        if index==0:
            #print(value)
            tlist=[]
            tlist.extend(value[0:3])
            #print(tlist)
            dic[index]=value[0:3]
        else:
            #print(index)
            val=temp_list[index]
            #print(val)
            #counter=len(val)
            new_l=[]
            counter=3
            for i,j in enumerate(val):
                #print(j)
                if counter>0:
                    if j not in tlist:
                        new_l.append(j)
                        tlist.append(j)
                        
                        #val.pop(j)
                        counter=counter-1
                    elif j in tlist:
                        pass
                elif counter==0:
                    break
            dic[index]=new_l
                
    
    #chunks_of_list=list(list_chunks(tlist,3))
    if modelname=='DBSCAN':
        dic={k-1:v for k,v in dic.items()}
    final_dict={"Cluster"+' '+str(k):v for k,v in dic.items()}
    if modelname=='DBSCAN':
        final_dict.update({"Cluster -1": ["Noise Points"]})
        
    return final_dict


def selected_topics(lda, vectorizer_lda, top_n=10):
    topic_dict={}
    for idx, topic in enumerate(lda.components_):
        topic_dict["Topic" +str(idx)]=[(vectorizer_lda.get_feature_names()[i], topic[i])
                        for i in topic.argsort()[:-top_n - 1:-1]]
    
    top_words_list=[]
    for x,y in topic_dict.items():
        if isinstance(y,list):
            top_words_list.append(max(y, key = lambda i : i[1])[0])
    
    return top_words_list

def main(data_q,num_topics=None,ngram_range=None,modelname=None): 
    cluster_dict_interim={}  
    if num_topics==None or num_topics<0:
        num_topics=3
    else:
        num_topics=num_topics
    
    if ngram_range==None:
        ngram_range=(2,2)
    elif not isinstance(ngram_range,tuple):
         ngram_range=(2,2)
    else:
         ngram_range=ngram_range
        
        
    for x in sorted(data_q['Predicted Clusters'].unique()):
        print(x)
        new=data_q[data_q['Predicted Clusters']==x]['All_Text']
        new.reset_index(inplace=True, drop=True)
        vectorizer_lda=TfidfVectorizer(lowercase=True,ngram_range=ngram_range)
        
        try:
            data_vectorized = vectorizer_lda.fit_transform(new)
        except ValueError as e:
            print(e)
            new = new.apply(lambda word: "{}{}".format(word,' Cluster Invalid. Text field is blank.'))
            data_vectorized = vectorizer_lda.fit_transform(new)
        lda = LatentDirichletAllocation(n_components=num_topics, max_iter=10, learning_method='online',verbose=True)
        data_lda = lda.fit_transform(data_vectorized)
        topic_modelled=selected_topics(lda, vectorizer_lda)
        cluster_dict_interim.update({"Cluster"+' '+ str(x):topic_modelled})
    cluster_dict=only_unique_clusteruniquevalues(cluster_dict_interim.keys(),cluster_dict_interim.values(),modelname=modelname)
    cluster_dict={key:value.append("Cluster name not generated with the n-gram combination selected. Please change n-gram selection or enter your custom cluster name") if len(value)<1 or value==None else value for key,value in cluster_dict.items()}
    
    return cluster_dict
        

# Keywords for topics clustered by Latent Dirichlet Allocation
