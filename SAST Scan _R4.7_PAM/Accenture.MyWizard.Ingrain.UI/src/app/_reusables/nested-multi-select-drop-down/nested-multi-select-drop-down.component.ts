import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormBuilder, FormGroup } from '@angular/forms';
import { element } from 'protractor';

@Component({
  selector: 'app-nested-multi-select-drop-down',
  templateUrl: './nested-multi-select-drop-down.component.html',
  styleUrls: ['./nested-multi-select-drop-down.component.scss']
})

export class NestedMultiSelectDropDownComponent implements OnInit {

  @Input() parentColumnData;
  @Input() childColumnData;
  @Input() columnData;
  @Input() displayCount;
  @Output() outputChildData = new EventEmitter();
  @Output() outputParentData = new EventEmitter();
  @Output() removeData = new EventEmitter();
  @Output() remainingDataPostRemove = new EventEmitter();

  seperatorParentChild = '<--->';

  filterAttributes = []; // shadow input parentColumnData
  filterOptions = {}; // shadow input childColumnData
  filters = {}; // shadow input columnData

  filterSelected: string[] = []; // Ngmodel bind two way 
  SelectedAttribute = new Set([]); // Selected Attributes
  SelectedFiltersForAttribute = new Set([]); // Selected Applied Filters to respective Attributes

  isExpandFilter: boolean[] = []; // To expand the Attribute
  filterGroup = []; // Array Structure [ { name : filterAttrs : [ { name: ele, checked: true }, ..... ] }, .... ] 
  filterLabels = []; // Array Structure [{ name: parent + ": " + element, value: element }, ...]
  checkedOptions = []; // Not in use
  selectedOptions = new FormControl(); // Not in use
  defaultDisplayCount = 5;
  constructor(private _formBuilder: FormBuilder) { }

  ngOnInit() {
    if ( this.displayCount ) {
      this.defaultDisplayCount = this.displayCount;
    }
    this.filterAttributes = this.parentColumnData;
    this.filterOptions = this.childColumnData;
    this.filters = this.columnData;
    this.setData();
  }

  onChangeOfSelect(event) {
    console.log(event);
    let parent;
    let child;
    this.filterLabels = [];
    this.resetChecks();
    this.filterSelected.forEach(element => {

      const index = element.indexOf("SelectAll");
      // parent = this.findAttribute(element);
      if (index < 0) {
        parent = element.split(this.seperatorParentChild)[0];
        child = element.split(this.seperatorParentChild)[1];
      } else {
        parent = element.split('SelectAll')[1];
      }

      if (parent) {
        this.isExpandFilter[parent] = true;
        if (index < 0)
          this.filterLabels.push({ name: parent + ": " + child, value: child });

        this.filterGroup.forEach(paraEl => {
          if (paraEl.name === parent) {
            paraEl['filterAttrs'].forEach(childEl => {
              if (childEl.name === child) {
                childEl.checked = true;
              }
            })
          }
        });

        this.SelectedAttribute.add(parent);
      }
    });

    if (this.SelectedAttribute) {
      this.outputParentData.emit(Array.from(this.SelectedAttribute));
      Array.from(this.SelectedAttribute).forEach(parent => {
        this.SelectedFiltersForAttribute.clear();
        this.filterGroup.forEach((paraEl, i) => {
          if (paraEl.name === parent) {
            this.filterGroup[i]['filterAttrs'].forEach(childEl => {
              if (childEl.checked) {
                this.SelectedFiltersForAttribute.add(childEl.name);
              }
            });
            this.outputChildData.emit({ parent: parent, child: Array.from(this.SelectedFiltersForAttribute) });
            if (this.SelectedFiltersForAttribute.size === 0)
              this.isExpandFilter[parent] = false;
          }
        });
      });
    }
  }

  findAttribute(child) {
    let parent;
    this.filterGroup.forEach((parentEl, parentIndex) => {
      this.filterGroup[parentIndex]['filterAttrs'].forEach(childEl => {
        if (childEl.name === child){
          parent = parentEl['name'];
        }
      });
    });
    return parent;
  }

  // onChangeMatOption(parent, child) {
  //   console.log(parent);
  //   console.log(child);
  //   console.log(this.filterSelected);
  // }

  // toggleParent(event, parent) {
  //   console.log(parent);
  //   let selectChild = [];
  //   if (event.checked) {
  //     parent['filterAttrs'].forEach(element => {
  //       element.checked = true;
  //       if (!this.hasFilterSelected(element.name)) {
  //         selectChild.push(element.name);
  //       }
  //     });
  //     this.filterSelected = this.filterSelected.concat(selectChild);
  //   } else {
  //     let index;
  //     selectChild  = Object.assign([], this.filterSelected);
  //     parent['filterAttrs'].forEach(element => {
  //       element.checked = false;
  //       index = selectChild.indexOf(element.name, 0);
  //       if (index > -1) {
  //         selectChild.splice(index, 1);
  //       }
  //     });
  //     this.filterSelected = selectChild;
  //   }
  // }

  // toggleChild(event, parent, child) {
  //   console.log(child);
  //   let label: string;
  //   if (event.checked) {
  //     label = parent.name + ": " + child.name;
  //     this.filterSelected.push(label);
  //   }
  // }

  setData () {
    this.filterLabels = [];
    Object.keys(this.filters).forEach((element, index) => {
      if (this.filters.hasOwnProperty(element)) {
        this.filterGroup.push({ name: element, filterAttrs: [] });
        Object.keys(this.filters[element]).forEach((ele, ind) => {
          if (this.filters[element][ele] === "True") {
            this.filterGroup[index]['filterAttrs'].push({ name: ele, checked: true });
            this.filterSelected.push(element + this.seperatorParentChild + ele);
            this.filterLabels.push({ name: element + ": " + ele, value: ele });
            this.isExpandFilter[element] = true;
          } else {
            this.filterGroup[index]['filterAttrs'].push({ name: ele, checked: false });
            // this.isExpandFilter[element] = false;
          }
        });
      }
    });
  }

  expandFilters(filterName: any, event) {
    // expand only selected parent dropdown category with that childs
    let index;
    let filterSel = [];
    let filterLab = [];
    if (event.checked) {
      this.isExpandFilter[filterName] = true;
    } else {
      this.isExpandFilter[filterName] = false;
      // Need to test once
      /* filterSel = Object.assign([], this.filterSelected);
      filterLab = Object.assign([], this.filterLabels);
      this.isExpandFilter[filterName] = false;
      this.filterGroup.forEach(paraEl => {
        if (paraEl.name === filterName) {
          paraEl['filterAttrs'].forEach(childEl => {
            childEl.checked = false;
            index = filterSel.indexOf(childEl.name, 0);
            if (index > -1) {
              filterSel.splice(index, 1);
              filterLab.splice(index, 1);
            }
          })
        }
      });
      this.filterSelected = filterSel;
      this.filterLabels = filterLab;
      */
    }
  }

  // Removes filter labels and child options
  removeFilter(event, remove) {
    event.stopPropagation();
    let index;
    let selectChild = [];
    let parent;
    let child;
    selectChild = Object.assign([], this.filterSelected);
    index = selectChild.indexOf(remove, 0);
    if (index > -1) {
      selectChild.splice(index, 1);
      // this.filterLabels.splice(index, 1);
      parent = remove.split(this.seperatorParentChild)[0];
      child = remove.split(this.seperatorParentChild)[1];
      this.filterLabels = this.filterLabels.filter(filter =>
        filter.name !== parent + ': ' + child);
    }
    this.filterSelected = selectChild;
    this.remainingDataPostRemove.emit(this.filterSelected);
    // parent = this.findAttribute(remove);
    this.removeData.emit({parent: parent, child: child});
    this.filterGroup.forEach((element, index) => {
      if (element.name === parent) {
        this.filterGroup[index]['filterAttrs'].forEach(ele => {
          if (ele.name === child) {
            ele.checked = false;
          }
        });
      }
    });
  }

  // hasFilterSelected(filter) {
  //   let has = false;
  //   this.filterSelected.forEach(element => {
  //     if (element === filter) { has = true; }
  //   });
  //   if (has) return true; else return false;
  // }

  resetChecks() {
    this.filterGroup.forEach((element, index) => {
      element['filterAttrs'].forEach(ele => {
        ele.checked = false;
      });
    })
  }

  selectAllFiltersForAttribute(ev, data, parentIndex) {
    console.log(data);
    let parent= this.filterGroup[parentIndex].name;
    if (ev._selected) {
      // this.filterLabels = [];
      this.resetChecks();
      this.SelectedAttribute.add(parent);
      this.outputParentData.emit(Array.from(this.SelectedAttribute));
      this.applyAllFiltersToSelectedAttribute(parentIndex, data.name, true);
      this.isExpandFilter[data.name] = true;
      ev._selected = true;
    }
    if(ev._selected==false){
      this.SelectedAttribute.delete(parent);
      this.outputParentData.emit(Array.from(this.SelectedAttribute));
      this.applyAllFiltersToSelectedAttribute(parentIndex, data.name, false);
      this.isExpandFilter[data.name] = false;
    }
  }


  removeDuplicateFromArray(data) {
    let filteredArray = new Set(data);
    return Array.from(filteredArray);
  }

  applyAllFiltersToSelectedAttribute(parentIndex: number, childName, condition) {
    const attributeName = this.filterGroup[parentIndex].name;
    this.filterGroup[parentIndex]['filterAttrs'] = [];

    if (condition) {
      Object.keys(this.filters[attributeName]).forEach((ele, ind) => {
        this.filterGroup[parentIndex]['filterAttrs'].push({ name: ele, checked: condition });
        this.filterSelected.push(childName + this.seperatorParentChild + ele);
        const index2 = this.filterLabels.findIndex(data => data.name === childName + ": " + ele);
        if (index2 < 0) {
          this.filterLabels.push({ name: childName + ": " + ele, value: ele });
        }
        this.SelectedFiltersForAttribute.add(ele);
      });
      this.outputChildData.emit({ parent: attributeName, child: Array.from(this.SelectedFiltersForAttribute) });
    } else {
      Object.keys(this.filters[attributeName]).forEach((ele, ind) => {
        this.filterGroup[parentIndex]['filterAttrs'].push({ name: ele, checked: condition });
        const index = this.filterSelected.findIndex(data => data === childName + this.seperatorParentChild + ele);
        const index2 = this.filterLabels.findIndex(data => data.name === childName + ": " + ele);
        this.filterSelected.splice(index, 1);
        this.filterLabels.splice(index2, 1);
        this.SelectedFiltersForAttribute.delete(ele);
      });
      this.outputChildData.emit({ parent: attributeName, child: [] });
    }
  }
}
