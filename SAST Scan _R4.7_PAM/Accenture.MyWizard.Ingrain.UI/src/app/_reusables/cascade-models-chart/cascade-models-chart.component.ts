import { Component, OnInit, ElementRef, ViewChild, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import * as d3 from 'd3';
import { Router } from '@angular/router';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { CookieService } from 'ngx-cookie-service';
import d3Tip from 'd3-tip';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';

@Component({
  selector: 'app-cascade-models-chart',
  templateUrl: './cascade-models-chart.component.html',
  styleUrls: ['./cascade-models-chart.component.scss']
})
export class CascadeModelsChartComponent implements OnChanges {

  constructor(private router: Router, private ls: LocalStorageService,
    private _cookieService: CookieService, private cus: CoreUtilsService) { }

  @ViewChild('cascadeChart', { static: true }) element: ElementRef;
  @Input() targetId: number;
  @Input() sourceId: number;
  @Input() models;
  @Input() modelNames;
  @Output() uniqueIdClicked = new EventEmitter();
  @Input() targetIdArray;
  @Input() uniqueIdArray;
  @Input() masterData;
  @Input() svgWidth;
  @Input() svgHeight;
  @Input() customCascade;
  htmlElement: HTMLElement;
  host;
  svg;
  width;
  height;
  nodeWidth = 160;
  nodeHeight = 40;
  verticalMargin = 10;
  horizontalMargin = 90;
  graphInitialxVal = 20;
  graphIntitialyVal = 20;
  dataForNodes = [];
  dataForLinks = [];
  target1;
  target2;
  dataForVerticalLines = [];
  dataForHeadToColumnVerticalLines = [];
  // headModels = [];
  headModelHeight = 60;
  headToColumnHeight = 100;
  dataForHeadNodes = [];
  pointerArray = [];
  dataForUniqueIdentifier = [];
  dataforUniqueIdentifierSourcePointer = [];
  dataforUniqueIdentifierTargetPointer = [];
  elem: any;

  /* ngOnInit() {
    this.htmlElement = this.element.nativeElement;
    this.host = d3.select(this.htmlElement);
    if (this.targetIdArray === null || this.targetIdArray === undefined) {
      this.targetIdArray = [undefined, undefined, undefined, undefined];
    }
    if (this.uniqueIdArray === null || this.uniqueIdArray === undefined) {
      this.uniqueIdArray = [[undefined, undefined], [undefined, undefined], [undefined, undefined], [undefined, undefined]];
    }
   // this.headModels = ['Start', 'Intermediate 1', 'Intermediate 2', 'Intermediate 3', 'End'];
    this.buildHeadNodesData(this.modelNames);
    this.buildNodesData(this.models);
    this.buildLinksData(this.targetIdArray);
    this.buildUniqueIdentifierLinksData(this.uniqueIdArray);
    this.buildVerticalLinesData(this.models);
    this.headToColumnVerticalLinesData(this.dataForHeadNodes);
    this.drawChart();
  } */

  ngOnChanges() {
    this.htmlElement = this.element.nativeElement;
    this.host = d3.select(this.htmlElement);
    this.width = this.svgWidth;
    this.height = this.svgHeight;
   // this.headModels = ['Start', 'Intermediate 1', 'Intermediate 2', 'Intermediate 3', 'End'];
    this.buildHeadNodesData(this.modelNames);
    this.buildNodesData(this.models);
    this.buildLinksData(this.targetIdArray);
  //  this.buildUniqueIdentifierLinksData(this.uniqueIdArray);
    this.buildVerticalLinesData(this.models);
    this.headToColumnVerticalLinesData(this.dataForHeadNodes);
    this.drawChart();
  }

  draw(targetArray) {
  //  this.changeModelSequence(targetArray);
    this.buildNodesData(this.models);
    this.buildLinksData(targetArray);
  //  this.buildUniqueIdentifierLinksData(this.uniqueIdArray);
    this.buildVerticalLinesData(this.models);
    this.drawChart();
  }

  /* changeModelSequence(targetArray) {
    targetArray.forEach((targetId, i) => {
      if (targetId) {
        const targetItem = this.models[i + 1].slice(targetId, targetId + 1);
        this.models.unshift(targetItem);
      }
    });
  } */

  buildNodesData(models) {
    this.dataForNodes = [];
    let id = 0;
    models.forEach((model, i) => {
      let node;
      let targetColIndex;
      model.forEach((text, j) => {
          const modelValues = this.masterData[i];
          const targetCol = modelValues[0]['targetColumn'];
          if (targetCol !== text) { // Added this condition to remove target column from attributes list but keeping the id
            if (targetColIndex === undefined) {
              node = {
                'id': id,
                'name': text,
                'x': this.graphInitialxVal + (i * (this.nodeWidth + this.horizontalMargin)),
                'y': (this.headModelHeight + this.headToColumnHeight) + (j * (this.nodeHeight + this.verticalMargin))
              };
            } else {
              node = {
                'id': id,
                'name': text,
                'x': this.graphInitialxVal + (i * (this.nodeWidth + this.horizontalMargin)),
                'y': (this.headModelHeight + this.headToColumnHeight) + ((j - 1) * (this.nodeHeight + this.verticalMargin))
              };
            }
            this.dataForNodes.push(node);
          } else {
            targetColIndex = j;
          }
        id++;
      });
    });
  }

  buildHeadNodesData(modelsName) {
    this.dataForHeadNodes = [];
    let id = 0;
    modelsName.forEach((text, i) => {
      const node = {
            'id': id,
            'name': text.modelName,
            'index': text.modelIndex,
            'x': this.graphInitialxVal + (i * (this.nodeWidth + this.horizontalMargin)),
            'y': this.graphIntitialyVal,
            'correlationId': text.correlationId,
            'selectedTrainedModelName': text.selectedTrainedModelName,
            'targetColumn': text.targetColumn
          };
        this.dataForHeadNodes.push(node);
        id++;
    });
  }

  buildLinksData(targetArray) {
    this.dataForLinks = [];
    this.pointerArray = [];
    targetArray.forEach((targetId, i) => {
      let link = {
        source: { x: 0, y: 0 },
        target: { x: 0, y: 0 },
        uniqueIdData:   {sourceName : '', targetName : ''}
      };
      let pointer = {
        target: { x: 0, y: 0 }
      };
      let sourceId;
      if (i < this.models.length) {
        if (i === 0) {
          sourceId = 0;
        } else {
          sourceId = this.models[i - 1].length;
        }
        if (targetId) {
          let sourceUnique;
          let targetUnique;
          if (this.uniqueIdArray[i][0] !== undefined && this.uniqueIdArray[i][1] !== undefined) {
            const sourceUniqueId = this.uniqueIdArray[i][0];
            const targetUniqueId = this.uniqueIdArray[i][1];
            sourceUnique = this.dataForNodes.filter(val => val.id === sourceUniqueId);
            targetUnique = this.dataForNodes.filter(val => val.id === targetUniqueId);
          }
          const source = this.dataForNodes.filter(val => val.id === sourceId);
          const target = this.dataForNodes.filter(val => val.id === targetId);
          link = {
            source: {
            x: this.graphIntitialyVal + (this.headModelHeight / 2),
            y: this.graphInitialxVal + this.nodeWidth + (i * (this.nodeWidth + this.horizontalMargin))
            },
            target: {
            x: target[0].y + (this.nodeHeight / 2),
            y: target[0].x
            },
            uniqueIdData: {
              sourceName : sourceUnique[0].name,
              targetName : targetUnique[0].name
            }
          };
          pointer = {
            target: {
              x: target[0].x,
              y: target[0].y + (this.nodeHeight / 2)
            }
          };
        }
        this.dataForLinks.push(link);
        this.pointerArray.push(pointer);
      }
    });
    this.htmlElement.innerHTML = '';
    this.drawChart();
    /* const data = [{
      source: {
      x: 40,
      y: 170
      },
      target: {
      x: 220,
      y: 250
      }
    }]; */
  }

  buildUniqueIdentifierLinksData(uniqueIdentifierArray) {
      this.dataForUniqueIdentifier = [];
      this.dataforUniqueIdentifierSourcePointer = [];
      this.dataforUniqueIdentifierTargetPointer = [];
      uniqueIdentifierArray.forEach((uniqueIdArr, i) => {
        let link = {
          source: { x: 0, y: 0 },
          target: { x: 0, y: 0 }
        };
        let sourcePointer = {
          source: { x: 0, y: 0 }
        };
        let targetPointer = {
          target: { x: 0, y: 0 }
        };
        const sourceId = uniqueIdArr[0];
        const targetId = uniqueIdArr[1];
        /* if (i === 0) {
          sourceId = 0;
        } else {
          sourceId = this.models[i - 1].length;
        } */
        if (uniqueIdArr[0] !== undefined && uniqueIdArr[1] !== undefined) {
          const source = this.dataForNodes.filter(val => val.id === sourceId);
          const target = this.dataForNodes.filter(val => val.id === targetId);
          link = {
            source: {
            x: source[0].y + (this.nodeHeight / 2),
            y: source[0].x + this.nodeWidth
            },
            target: {
            x: target[0].y + (this.nodeHeight / 2),
            y: target[0].x
            }
          };
          sourcePointer = {
            source: {
              x: source[0].x + this.nodeWidth,
              y: source[0].y + (this.nodeHeight / 2)
            }
          };
          targetPointer = {
            target: {
              x: target[0].x,
              y: target[0].y + (this.nodeHeight / 2)
            }
          };
        }
        this.dataForUniqueIdentifier.push(link);
        this.dataforUniqueIdentifierSourcePointer.push(sourcePointer);
        this.dataforUniqueIdentifierTargetPointer.push(targetPointer);
      });
      this.htmlElement.innerHTML = '';
      this.drawChart();
  }

  buildVerticalLinesData(models) {
   this.dataForVerticalLines = [];
   const modelLengthArray = [];
   let previousLength = 0;
   models.forEach(model => {
     let length;
     if(this.customCascade == true) {
      length = previousLength + (model.length);
     } else {
      length = previousLength + (model.length - 1); // model.length - 1 becaause target column is removed
     }
      modelLengthArray.push(length);
      previousLength = length;
   });
      let previousColumn;
      this.dataForNodes.forEach((column, j) => {
        if (previousColumn && modelLengthArray.includes(j) === false) {
          const link = {
              source: {
                x: previousColumn.x + (this.nodeWidth / 2),
                y: previousColumn.y + this.nodeHeight
              },
              target: {
                x: column.x + (this.nodeWidth / 2),
                y: column.y
              }
          };
          this.dataForVerticalLines.push(link);
        }
        previousColumn = column;
      });
  }

  headToColumnVerticalLinesData(models) {
    this.dataForHeadToColumnVerticalLines = [];
    this.dataForHeadNodes.forEach((column, j) => {
        const link = {
            source: {
              x: column.x + (this.nodeWidth / 2),
              y: column.y + this.headModelHeight
            },
            target: {
              x: column.x + (this.nodeWidth / 2),
              y: column.y + this.headModelHeight + this.headToColumnHeight - (this.nodeHeight / 2)
            }
        };
        this.dataForHeadToColumnVerticalLines.push(link);
    });
  }

  drawChart() {

    this.host.html('');
    this.svg = this.host.append('svg')
      .attr('width', this.width)
      .attr('height', this.height);

    const heightOfNode = this.nodeHeight;
    const widthOfNode = this.nodeWidth;
    const headNodeHeight = this.headModelHeight;
    const headToColumnHeight = this.headToColumnHeight;
   // const arrowImage = '<symbol viewBox='0 0 129 129' enable-background='new 0 0 129 129' id='ingrAI-back-icon'><g><path d='m88.6,121.3c0.8,0.8 1.8,1.2 2.9,1.2s2.1-0.4 2.9-1.2c1.6-1.6 1.6-4.2 0-5.8l-51-51 51-51c1.6-1.6 1.6-4.2 0-5.8s-4.2-1.6-5.8,0l-54,53.9c-1.6,1.6-1.6,4.2 0,5.8l54,53.9z'></path></g></symbol>'

   /* let defs = this.svg.append('defs')
   .append('marker')
   .attr({
     'id': 'arrow',
     'viewBox': '0 -5 10 10',
     'refX': 5,
     'refY': 0,
     'markerWidth': 4,
     'markerHeight': 4,
     'orient': 'auto'
    })
    .append('path')
    .attr('d', 'M14.81,1.33a10,10,0,0,1,3.61,3.61,9.56,9.56,0,0,1,1.33,4.94,9.52,9.52,0,0,1-1.33,4.93,9.92,9.92,0,0,1-3.61,3.61,9.52,9.52,0,0,1-4.93,1.33,9.56,9.56,0,0,1-4.94-1.33,10,10,0,0,1-3.61-3.61A9.52,9.52,0,0,1,0,9.88,9.56,9.56,0,0,1,1.33,4.94,10.07,10.07,0,0,1,4.94,1.33,9.56,9.56,0,0,1,9.88,0,9.52,9.52,0,0,1,14.81,1.33ZM9,3.78a.94.94,0,0,0-.68-.28.81.81,0,0,0-.63.28L7,4.46a1,1,0,0,0-.32.7A.84.84,0,0,0,7,5.81l4,4.07L7,13.94a.87.87,0,0,0,0,1.35l.68.64a.84.84,0,0,0,.65.32A.87.87,0,0,0,9,16l5.42-5.42a.93.93,0,0,0,.28-.67,1,1,0,0,0-.28-.68Z')
    .attr('class', 'arrowHead'); */
    const _this = this;

    const link = d3.linkHorizontal()
      .x(function(d) {
        return d.y;
      })
      .y(function(d) {
        return d.x;
      });

      const linkVertical = d3.linkVertical()
      .x(function(d) { return d.x; })
      .y(function(d) { return d.y; });

      const linkRadial = d3.linkRadial()
      .angle(function(d) {
          return d.x;
      })
      .radius(function(d) {
          return d.y;
      });

    /* this.svg.selectAll(null)
      .data(this.dataForLinks)
      .enter()
      .append('path')
      .attr('fill', 'none')
      .attr('stroke', '#10ADD3')
      .attr('d', link); */
      const rx = 3;
      const ry = 3;
      const hl = this.horizontalMargin / 2;
      const hlUnique = this.horizontalMargin / 3;
      const remaininghlUnique = this.horizontalMargin - hlUnique;

      const tip = d3Tip();

      tip.attr('class', 'd3-tip')
      .html(function(d) {
        if (d.hasOwnProperty('uniqueIdData')) {
          if (d.uniqueIdData.sourceName !== '' && d.uniqueIdData.targetName !== '') {
            return `<div>
                    <div><strong>Source Unique Identifier:</strong> <span>${d.uniqueIdData.sourceName}</span></div>
                    <div><strong>Target Unique Identifier:</strong> <span>${d.uniqueIdData.targetName}</span></div>
                  </div>`;
          }
        } else if (d.hasOwnProperty('targetColumn')) {
          return `<span><strong>Target Column:</strong>${d.targetColumn}</span>`;
        }
      });

      /* const tip = d3.tip()
      .data(this.dataForUniqueIdentifier)
      .attr('class', 'd3-tip')
      .offset([-10, 0])
      .html(function(d) {
        if (d.name.sourceName !== '' && d.name.targetName !== '') {
          return `<div>
                  <div><strong>Source Unique Identifier:</strong> <span style='color:red'>${d.name.sourceName}</span></div>
                  <div><strong>Target Unique Identifier:</strong> <span style='color:red'>${d.name.targetName}</span></div>
                </div>`;
        }
      }); */

    this.svg.call(tip);

      this.svg.selectAll(null)
      .data(this.dataForLinks)
      .enter()
      .append('path')
      .attr('fill', 'none')
      .attr('class', 'mappingLink')
      .attr('stroke', '#409249')
      .attr('stroke-width', '2px')
      .attr('d', function(item) {
        if (item.source.y !== 0) {
          return `
          M${item.source.y - rx},${item.source.x}
          h${hl - rx}
          a${rx},${ry} 0 0 1 ${rx},${ry}
          v${item.target.x - item.source.x - (2 * rx)}
          a${rx},${ry} 1 0 0 ${rx},${ry}
          h${hl}
        `;
        }
      }).on('click', function (item, i) {
        _this.uniqueIdentifierClick(i);  // to pass which target link is clicked
      }).on('mouseover', function (d, i) {
        d3.select(this).transition()
             .duration('50')
             .attr('stroke-width', '3px')
             .attr('cursor', 'pointer');
        tip.show(d, this);
      })
      .on('mouseout', function (d, i) {
        d3.select(this).transition()
             .duration('50')
             .attr('stroke-width', '2px');
        tip.hide(d, this);
      });

      this.svg.selectAll(null)
      .data(this.dataForUniqueIdentifier)
      .enter()
      .append('path')
      .attr('fill', 'none')
      .attr('class', 'uniqueIdLink')
      .attr('stroke', '#9657D5')
      .attr('stroke-width', '2px')
      .attr('d', function(item) {
        if (item.source.y !== 0) {
          console.log('item' + item);
          if (item.source.x > item.target.x) {
            return `
              M${item.source.y - rx},${item.source.x}
              h${hlUnique - rx}
              a${rx},${-ry} 1 0 0 ${rx},${-ry}
              v${item.target.x - item.source.x + (2 * rx)}
              a${rx},${-ry} 0 0 1 ${rx},${-ry}
              h${remaininghlUnique}
            `;
          } else if (item.source.x === item.target.x) {
            return `
              M${item.source.y - rx},${item.source.x}
              h${_this.horizontalMargin}
            `;
          } else {
            return `
              M${item.source.y - rx},${item.source.x}
              h${hlUnique - rx}
              a${rx},${ry} 0 0 1 ${rx},${ry}
              v${item.target.x - item.source.x - (2 * rx)}
              a${rx},${ry} 1 0 0 ${rx},${ry}
              h${remaininghlUnique}
            `;
          }
        }
      });

      this.svg.selectAll(null)
      .data(this.dataForVerticalLines)
      .enter()
      .append('path')
      .attr('class', 'verticalLines')
      .attr('fill', 'none')
      .attr('stroke', '#543fba')
      .attr('d', linkVertical);

      this.svg.selectAll(null)
      .data(this.dataForHeadToColumnVerticalLines)
      .enter()
      .append('path')
      .attr('class', 'headToColumnVerticalLines')
      .attr('fill', 'none')
      .attr('stroke', '#543fba')
      .attr('d', linkVertical);

      this.svg.selectAll(null)
      .data(this.pointerArray)
      .enter()
      .append('circle')
      .attr('class', 'endPoint')
      .attr('cx', function (d) { return d.target.x; })
      .attr('cy', function (d) { return d.target.y; })
      .attr('r', '3')
      .attr('fill', function(item) {
        if (item.target.x !== 0) {
          return '#409249';
        } else {
          return 'none';
        }
      });

      this.svg.selectAll(null)
      .data(this.dataforUniqueIdentifierSourcePointer)
      .enter()
      .append('circle')
      .attr('class', 'endPoint')
      .attr('cx', function (d) { return d.source.x; })
      .attr('cy', function (d) { return d.source.y; })
      .attr('r', '3')
      .attr('fill', function(item) {
        if (item.source.x !== 0) {
          return '#9657D5';
        } else {
          return 'none';
        }
      });

      this.svg.selectAll(null)
      .data(this.dataforUniqueIdentifierTargetPointer)
      .enter()
      .append('circle')
      .attr('class', 'endPoint')
      .attr('cx', function (d) { return d.target.x; })
      .attr('cy', function (d) { return d.target.y; })
      .attr('r', '3')
      .attr('fill', function(item) {
        if (item.target.x !== 0) {
          return '#9657D5';
        } else {
          return 'none';
        }
      });

      const div = d3.select('body').append('div')
                  .attr('class', 'cascade-tooltip')
                  .style('opacity', 0)
                  .style('z-index', 1051);

      const fontSizeOfNode = 12;

      let node = this.svg.append('g')
        .attr('class', 'nodes')
        .attr('font-family', 'sans-serif')
        .attr('font-size', fontSizeOfNode)
        .selectAll('g');

        node = node
            .data(this.dataForNodes)
            .enter().append('g');

            node.append('rect')
            .attr('x', function (d) { return d.x; })
            .attr('y', function (d) { return d.y; })
            .attr('rx', '6')
            .attr('ry', '6')
            .attr('height', this.nodeHeight)
            .attr('width', this.nodeWidth)
            .attr('fill', 'none')
            .attr('stroke', '#543fba');

            node.append('text')
            .attr('x', function (d) { return d.x + (widthOfNode / 2); })
            .attr('y', function (d) { return d.y + (heightOfNode / 2) + (fontSizeOfNode / 2); })
            .attr('text-anchor', 'middle')
            .text(function(d) { return d.name; })
            .each(wrap)
            .on('mouseover', function(d) {
              div.transition()
                  .duration(200)
                  .style('opacity', .9);
              div.html(d.name)
                  .style('left', (d3.event.pageX) + 'px')
                  .style('top', (d3.event.pageY - 28) + 'px');
              })
            .on('mouseout', function(d) {
                div.transition()
                    .duration(500)
                    .style('opacity', 0);
            });


            let headnode = this.svg.append('g')
            .attr('class', 'headnodes')
            .attr('font-family', 'sans-serif')
            .attr('font-size', 12)
            .selectAll('g');

            headnode = headnode
            .data(this.dataForHeadNodes)
            .enter().append('g');

            headnode.append('rect')
            .attr('x', function (d) { return d.x; })
            .attr('y', function (d) { return d.y; })
            .attr('rx', '6')
            .attr('ry', '6')
            .attr('height', this.headModelHeight)
            .attr('width', this.nodeWidth)
            .attr('fill', 'none')
            .attr('stroke', '#543fba');

            headnode.append('text')
            .attr('x', function (d) { return d.x + (widthOfNode / 2); })
            .attr('y', function (d) { return d.y + (heightOfNode / 2); })
            .attr('text-anchor', 'middle')
            .attr('fill', 'black')
            .attr('font-weight', '700')
            .text(function(d) { return d.index; })
            .on('mouseover', function (d, i) {
              d3.select(this).transition()
                   .duration('50')
                   .attr('font-weight', '900')
                   .attr('cursor', 'pointer');
              tip.show(d, this);
            })
            .on('mouseout', function (d, i) {
              d3.select(this).transition()
                   .duration('50')
                   .attr('font-weight', '700');
              tip.hide(d, this);
            });

            headnode.append('text')
            .attr('x', function (d) { return d.x + (widthOfNode / 2); })
            .attr('y', function (d) { return d.y + (headNodeHeight / 2) + 10; })
            .attr('text-anchor', 'middle')
            .attr('font-size', '10px')
            .text(function(d) { return d.name; })
            .each(wrap)
            .attr('cursor', 'pointer')
            .on('mouseover', function(d) {
              div.transition()
                  .duration(200)
                  .style('opacity', .9);
              div.html(d.name)
                  .style('left', (d3.event.pageX) + 'px')
                  .style('top', (d3.event.pageY - 28) + 'px');
              })
            .on('mouseout', function(d) {
                div.transition()
                    .duration(500)
                    .style('opacity', 0);
            })
            .on('click', function (item, i) {
              div.transition()
                    .duration(500)
                    .style('opacity', 0);
              _this.navigateToModel(item.correlationId, item.selectedTrainedModelName, item.name);
            });

            headnode.append('text')
            .attr('x', function (d) { return d.x + (widthOfNode / 1.5); })
            .attr('y', function (d) { return d.y + headNodeHeight + (headToColumnHeight / 3) + 10; })
            .attr('text-anchor', 'start')
            .attr('fill', '#543fba')
            .attr('font-weight', '700')
            .text(function(d) { return 'Attributes'; });

            headnode.append('circle')
            .attr('class', 'arrowHead')
            .attr('cx', function (d) { return d.x + widthOfNode; })
            .attr('cy', function (d) { return d.y + (headNodeHeight / 2); })
            .attr('r', '10')
            .attr('fill', function(item , i) {
              if (i !== (_this.models.length - 1)) {
                return '#ffffff';
              } else {
                return 'none';
              }
            });

            /* headnode.selectAll('.arrowHead').append('path')
            .attr('cx', function (d) { return d.x + widthOfNode; })
            .attr('viewBox', '0 0 129 129')
            .attr('enable-background', 'new 0 0 129 129')
            .attr('fill', '#10ADD3')
            .attr('stroke', '#10ADD3')
            .attr('d', 'M14.81,1.33a10,10,0,0,1,3.61,3.61,9.56,9.56,0,0,1,1.33,4.94,9.52,9.52,0,0,1-1.33,4.93,9.92,9.92,0,0,1-3.61,3.61,9.52,9.52,0,0,1-4.93,1.33,9.56,9.56,0,0,1-4.94-1.33,10,10,0,0,1-3.61-3.61A9.52,9.52,0,0,1,0,9.88,9.56,9.56,0,0,1,1.33,4.94,10.07,10.07,0,0,1,4.94,1.33,9.56,9.56,0,0,1,9.88,0,9.52,9.52,0,0,1,14.81,1.33ZM9,3.78a.94.94,0,0,0-.68-.28.81.81,0,0,0-.63.28L7,4.46a1,1,0,0,0-.32.7A.84.84,0,0,0,7,5.81l4,4.07L7,13.94a.87.87,0,0,0,0,1.35l.68.64a.84.84,0,0,0,.65.32A.87.87,0,0,0,9,16l5.42-5.42a.93.93,0,0,0,.28-.67,1,1,0,0,0-.28-.68Z'); */

            /* headnode.selectAll('.arrowHead')
            .append('image').attr('xlink:href', '#ingrAI-icon-circle-arrow'); */

            /* const circleSvg = headnode.append('svg')
            .attr('x', function (d) { return (d.x - 160) + 'px'; })
            .attr('y', (headNodeHeight - 20) + 'px')
            .attr('fill', '#10ADD3')
            .attr('viewBox', '0 0 600 600')
            .attr('enable-background', 'new 0 0 19.75 19.75'); */
            const circleSvg = headnode.append('g')
            .attr('fill', '#409249')
            .attr('transform', function (d) { return 'translate(' + (d.x + widthOfNode - 10) + ',' + (d.y + (headNodeHeight / 2) - 10) + ')'; })
            .attr('viewBox', '0 0 600 600')
            .attr('enable-background', 'new 0 0 19.75 19.75');

            const circleG = circleSvg.append('path')
            .data(this.models)
            .attr('cx', 20)
            .attr('d', function(item , i) {
              if (i !== (_this.models.length - 1)) {
                return 'M14.81,1.33a10,10,0,0,1,3.61,3.61,9.56,9.56,0,0,1,1.33,4.94,9.52,9.52,0,0,1-1.33,4.93,9.92,9.92,0,0,1-3.61,3.61,9.52,9.52,0,0,1-4.93,1.33,9.56,9.56,0,0,1-4.94-1.33,10,10,0,0,1-3.61-3.61A9.52,9.52,0,0,1,0,9.88,9.56,9.56,0,0,1,1.33,4.94,10.07,10.07,0,0,1,4.94,1.33,9.56,9.56,0,0,1,9.88,0,9.52,9.52,0,0,1,14.81,1.33ZM9,3.78a.94.94,0,0,0-.68-.28.81.81,0,0,0-.63.28L7,4.46a1,1,0,0,0-.32.7A.84.84,0,0,0,7,5.81l4,4.07L7,13.94a.87.87,0,0,0,0,1.35l.68.64a.84.84,0,0,0,.65.32A.87.87,0,0,0,9,16l5.42-5.42a.93.93,0,0,0,.28-.67,1,1,0,0,0-.28-.68Z';
              }
            });

            const imageWidth = 45;
            const imageHeight = 45;
            const imageRadius = imageWidth / 2;

            this.svg.selectAll(null)
            .data(this.dataForHeadNodes)
            .enter()
            .append('circle')
            .attr('class', 'image')
            .attr('cx', function (d) { return d.x + (widthOfNode / 2); })
            .attr('cy', function (d) { return d.y + headNodeHeight + (headToColumnHeight / 2) - 8; })
            .attr('r', imageRadius)
            .attr('fill', '#ffffff');

             /* const imgSvg = this.svg.selectAll(null)
            .data(this.dataForHeadNodes)
            .enter()
            .append('svg')
            .attr('x', function (d) { return d.x + (widthOfNode / 3); })
            .attr('y', function (d) { return d.y + headNodeHeight + (headToColumnHeight / 2) - 8; })
            .attr('fill', '#10ADD3')
            .attr('viewBox', '0 0 800 800')
            .attr('enable-background', 'new 0 0 19.75 19.75'); */

            const img = this.svg.selectAll(null)
            .data(this.dataForHeadNodes)
            .enter()
            .append('svg:image')
                .attr('xlink:href', 'assets/images/cascade-attr-icon.svg')
                .attr('width', imageWidth)
                .attr('height', imageHeight)
                .attr('x', function (d) { return d.x + (widthOfNode / 2) - imageRadius; })
                .attr('y', function (d) { return d.y + headNodeHeight + 20; })
                .attr('fill', '#543fba')
                .attr('viewBox', '0 0 800 800')
                .attr('enable-background', 'new 0 0 19.75 19.75');


            /* <symbol viewBox='0 0 19.75 19.75' enable-background='new 0 0 19.75 19.75' id='ingrAI-icon-circle-arrow'>
			<g>
				<path style='fill:#ffffff;'
					d='M14.81,1.33a10,10,0,0,1,3.61,3.61,9.56,9.56,0,0,1,1.33,4.94,9.52,9.52,0,0,1-1.33,4.93,9.92,9.92,0,0,1-3.61,3.61,9.52,9.52,0,0,1-4.93,1.33,9.56,9.56,0,0,1-4.94-1.33,10,10,0,0,1-3.61-3.61A9.52,9.52,0,0,1,0,9.88,9.56,9.56,0,0,1,1.33,4.94,10.07,10.07,0,0,1,4.94,1.33,9.56,9.56,0,0,1,9.88,0,9.52,9.52,0,0,1,14.81,1.33ZM9,3.78a.94.94,0,0,0-.68-.28.81.81,0,0,0-.63.28L7,4.46a1,1,0,0,0-.32.7A.84.84,0,0,0,7,5.81l4,4.07L7,13.94a.87.87,0,0,0,0,1.35l.68.64a.84.84,0,0,0,.65.32A.87.87,0,0,0,9,16l5.42-5.42a.93.93,0,0,0,.28-.67,1,1,0,0,0-.28-.68Z' />
			</g>
		</symbol> */

            /* let verticalLines = this.svg.append('g')
              .attr('class', 'verticallinks')
              .attr('font-family', 'sans-serif')
              .attr('font-size', 10)
              .selectAll('g');

              verticalLines = verticalLines
              .data(this.dataForVerticalLines)
              .enter().append('g');

              verticalLines.append('line')
              .attr('x', function(d) {return d.x; })
              .attr('y', function(d) {return d.y; })
              .attr('height', this.verticalMargin)
              .attr('width', '1px')
              .attr('fill', 'none')
              .attr('stroke', '#10ADD3'); */

    /* const formatNumber = d3.format(',.0f'),
    format = function (d: any) { return formatNumber(d) + ' TWh'; },
    color = d3.scaleOrdinal(d3.schemeCategory10);

    const sankey = d3Sankey.sankey()
    .nodeWidth(150)
    .nodePadding(2)
    .extent([[1, 1], [this.width - 1, this.height - 6]]);

    let link = this.svg.append('g')
        .attr('class', 'links')
        .attr('fill', 'none')
        .attr('stroke', '#000')
        .attr('stroke-opacity', 0.2)
        .selectAll('path');

    let node = this.svg.append('g')
        .attr('class', 'nodes')
        .attr('font-family', 'sans-serif')
        .attr('font-size', 10)
        .selectAll('g');

    const energy: DAG = {
        nodes: [{
            nodeId: 0,
            name: 'node0'
        }, {
            nodeId: 1,
            name: 'node1'
        }, {
            nodeId: 2,
            name: 'node2'
        }, {
            nodeId: 3,
            name: 'node3'
        }, {
            nodeId: 4,
            name: 'node4'
        }, {
            nodeId: 5,
            name: 'node5'
        }],
        links: [{
            source: 0,
            target: 2,
            value: 2,
            uom: 'Widget(s)'
        }, {
            source: 1,
            target: 3,
            value: 2,
            uom: 'Widget(s)'
        }, {
            source: 2,
            target: 4,
            value: 2,
            uom: 'Widget(s)'
        }, {
          source: 3,
          target: 5,
          value: 2,
          uom: 'Widget(s)'
      }]
    };


        sankey(energy);

         link = link
            .data(energy.links)
            .enter().append('path')
            .attr('d', d3Sankey.sankeyLinkHorizontal())
            .attr('stroke-width', 2);

        link.append('title')
            .text(function (d: any) { return d.source.name + ' → ' + d.target.name + '\n' + format(d.value); });

        node = node
            .data(energy.nodes)
            .enter().append('g');

        node.append('rect')
            .attr('x', function (d: any) { return d.x0; })
            .attr('y', function (d: any) { return d.y0; })
            .attr('height', this.nodeHeight)
            .attr('width', this.nodeWidth)
            .attr('fill', 'none')
            .attr('stroke', 'blue');

        node.append('text')
            .attr('x', function (d: any) { return (d.x0 + 75); })
            .attr('y', function (d: any) { return (d.y0 + 20); })
            .attr('text-anchor', 'middle')
            .text(function (d: any) { return d.name; });

        node.append('title')
            .text(function (d: any) { return d.name + '\n' + format(d.value); }); */
            function wrap() {
              let self = d3.select(this),
                  textLength = self.node().getComputedTextLength(),
                  text = self.text();
              while (textLength > 140 && text.length > 0) {
                  text = text.slice(0, -1);
                  self.text(text + '...');
                  textLength = self.node().getComputedTextLength();
              }
            }
    }

    uniqueIdentifierClick(index) {
      this.uniqueIdClicked.emit(index + 1);
    }

    navigateToModel(correlationId, selectedTrainedModelName, modelName) {
      if(this.customCascade !== true) {
        this.ls.setLocalStorageData('correlationId', correlationId);
        this._cookieService.delete('SelectedRecommendedModel');
        this._cookieService.set('SelectedRecommendedModel', selectedTrainedModelName);
        localStorage.setItem('SelectedRecommendedModel', selectedTrainedModelName);
        this.ls.setLocalStorageData('modelName', modelName);
        this.cus.allTabs = [
          {
            'mainTab': 'Problem Statement', 'status': '', 'routerLink': 'problemstatement/usecasedefinition', 'tabIndex': 0, 'subTab': [
              { 'childTab': 'Use Case Definition', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'breadcrumbStatus': '' }
            ]
          },
          {
            'mainTab': 'Data Engineering', 'status': '', 'routerLink': 'dataengineering/datacleanup', 'tabIndex': 1, 'subTab': [
              { 'childTab': 'Data Curation', 'status': 'active', 'routerLink': 'dataengineering/datacleanup', 'breadcrumbStatus': '' },
              { 'childTab': 'Data Transformation', 'status': 'disabled', 'routerLink': 'dataengineering/preprocessdata', 'breadcrumbStatus': '' }
            ]
          },
          {
            'mainTab': 'Model Engineering', 'status': 'active', 'routerLink': 'modelengineering/FeatureSelection', 'tabIndex': 2, 'subTab': [
              { 'childTab': 'Feature Selection', 'status': 'active', 'routerLink': 'modelengineering/FeatureSelection', 'breadcrumbStatus': 'completed' },
              { 'childTab': 'Recommended AI', 'status': 'active', 'routerLink': 'modelengineering/RecommendedAI', 'breadcrumbStatus': 'completed' },
              { 'childTab': 'Teach and Test', 'status': 'disabled', 'routerLink': 'modelengineering/TeachAndTest', 'breadcrumbStatus': 'active' },
              { 'childTab': 'Compare Test Scenarios', 'status': 'disabled', 'routerLink': 'modelengineering/CompareModels', 'breadcrumbStatus': 'disabled' }
            ]
          },
          {
            'mainTab': 'Deploy Model', 'status': 'disabled', 'routerLink': 'deploymodel/publishmodel', 'tabIndex': 3, 'subTab': [
              { 'childTab': 'Publish Model', 'status': 'active', 'routerLink': 'deploymodel/publishmodel', 'breadcrumbStatus': 'active' },
              { 'childTab': 'Deployed Model', 'status': 'disabled', 'routerLink': 'deploymodel/deployedmodel', 'breadcrumbStatus': 'disabled' }
            ]
          }
        ];
        const cascadedId = sessionStorage.getItem('cascadedId');
        this.router.navigate(['dashboard/modelengineering/TeachAndTest/WhatIfAnalysis'], {
          queryParams: {
            'cascadedId': cascadedId,
            'modelName': modelName
          }
        });
      }
    }

    zoom(zoomValue) {
    //  this.svg.attr('transform', 'scale('+zoomValue+')');
      this.htmlElement.setAttribute('style', 'zoom:'+zoomValue);
    }

    openFullscreen() {
      this.elem = this.htmlElement;
      if (this.elem.requestFullscreen) {
        this.elem.requestFullscreen();
      } else if (this.elem.mozRequestFullScreen) {
        /* Firefox */
        this.elem.mozRequestFullScreen();
      } else if (this.elem.webkitRequestFullscreen) {
        /* Chrome, Safari and Opera */
        this.elem.webkitRequestFullscreen();
      } else if (this.elem.msRequestFullscreen) {
        /* IE/Edge */
        this.elem.msRequestFullscreen();
      }
    }
}

/* interface SNodeExtra {
  nodeId: number;
  name: string;
}

interface SLinkExtra {
  source: number;
  target: number;
  value: number;
  uom: string;
}
type SNode = d3Sankey.SankeyNode<SNodeExtra, SLinkExtra>;
type SLink = d3Sankey.SankeyLink<SNodeExtra, SLinkExtra>;

interface DAG {
  nodes: SNode[];
  links: SLink[];
} */
