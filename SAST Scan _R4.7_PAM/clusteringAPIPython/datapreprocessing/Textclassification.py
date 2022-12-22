# -*- coding: utf-8 -*-
"""
Created on Fri Dec 13 10:58:40 2019

@author: ravi.kiran.sirigiri
"""

import warnings
warnings.filterwarnings("ignore")
import pandas as pd
# Laoding libraries - ML related
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.feature_extraction.text import TfidfVectorizer

from sklearn import cluster
from sklearn import metrics
from sklearn.cluster import KMeans
#os.chdir("C:\\Users\\ravi.kiran.sirigiri\\Documents\\Ingrain_NLP") #setting working directory
# Laoding libraries -Text Processing
from nltk.tag import pos_tag
from nltk.stem import PorterStemmer, WordNetLemmatizer, SnowballStemmer
import nltk
import re
from nltk.corpus import wordnet as wn
from nltk.corpus import stopwords
import string
# Laoding libraries -Utility
from functools import lru_cache
import numpy as np
from SSAIutils import utils
import spacy

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

def text_process1(t, language, spacy_vectors, pos=True, lemmetize=True, stem=True, stop_words=[]):
    if language!='thai':
        if stop_words != []:
            stop_words = [str(i).lower() for i in stop_words]
            stops = set(list(stopwords.words(language)) + (stop_words))

        else:
            # Defining stop words
            stops = set(stopwords.words(language))

        t = re.sub('[\n\t\r]+', ' ', t)  # Remove linebreak, tab, return
        t = t.lower()  # Convert to lower case
        t = re.sub('[^a-zA-Z]', ' ', t)  # Remove Non-letters
        # sentence = nltk.sent_tokenize(t)                      # Sentence wise tokenize
        # sentence=t
        modified_sentence = ""
        # for s in sentence:
        words = nltk.word_tokenize(t)  # Word Tokenization
        # nlp = spacy.load("en_core_web_sm")

        lemmatized = ""
        doc = spacy_vectors(t)
       # print("stopsstopsstopsstops...........",doc,".............stopsstops")
        for token in doc:
            if str(token) not in stops:
                if lemmetize:  # lemmatization
                    # print(tree_2_word(tw[1]))
                    lemma = token.lemma_
                # if (stem) & (not wn.synsets(lemma)):
                # lemma = porter.stem(w)
                if stem:
                    stemmer = SnowballStemmer(language)
                    if lemmetize:
                        lemma = stemmer.stem(lemma)
                    else:
                        lemma = stemmer.stem(str(token))
                if not lemmetize and not stem:
                    lemma = str(token)
           
                if lemma not in stops:  
                    lemmatized =  lemmatized + " " + lemma
        modified_sentence = modified_sentence + " " + lemmatized
        modified_sentence = modified_sentence.strip()
        t = re.sub('[' + string.punctuation + ']+', '', \
                   modified_sentence)  # Remove Punctuations
        t = re.sub('\s+\s+', ' ', t)  # Remove double whitespace
    return (t)

def text_process(t,pos=True,lemmetize=True,stem=True,stop_words=[]):
    if stop_words!=[]:
        stops = set(list(stopwords.words("english"))+(stop_words))
    else:
        stops = set(stopwords.words("english"))       # Defining stop words
    t = re.sub('[\n\t\r]+',' ',t)                         # Remove linebreak, tab, return
    t = t.lower()                                         # Convert to lower case
    t = re.sub('[^a-zA-Z]',' ',t)                         # Remove Non-letters
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
                        if (stem) & (not wn.synsets(lemma)):
                            lemma = porter.stem(tw[0])    # Stemming
                    else:
                        if stem:
                            lemma=porter.stem(tw[0])
                        else:
                            lemma=tw[0]
                    #print(lemmatized)
                    lemmatized=lemmatized+" "+lemma
        else:
            lemmatized= " "
            for w in words:
                if w not in stops:
                    if lemmetize:                         # lemmatization
                        lemma=lemmatize(w)
                        if (stem) & (not wn.synsets(lemma)):
                            lemma = porter.stem(w)   
                    else:
                        if stem:
                            lemma=porter.stem(w)
                        else:
                            lemma=w
                  
                    lemmatized=lemmatized+" "+lemma               
        modified_sentence=modified_sentence+" "+lemmatized
    t = re.sub('['+string.punctuation+']+','',\
               modified_sentence)                          # Remove Punctuations     
    t = re.sub('\s+\s+',' ',t)                             # Remove double whitespace
    return(t)
    
    
