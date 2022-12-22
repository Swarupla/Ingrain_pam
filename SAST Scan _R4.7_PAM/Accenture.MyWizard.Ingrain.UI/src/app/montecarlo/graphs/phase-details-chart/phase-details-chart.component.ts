import { Component, OnInit, ElementRef, ViewChild, Input, Output, OnChanges, EventEmitter } from '@angular/core';
import * as d3 from 'd3';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import * as moment from 'moment';
import * as _ from 'lodash';
const _moment = moment;

@Component({
  selector: 'app-phase-details-chart',
  templateUrl: './phase-details-chart.component.html',
  styleUrls: ['./phase-details-chart.component.scss']
})

export class PhaseDetailsChartComponent implements OnChanges {
  @ViewChild('containerLineChart', { static: true }) containerLineChart: ElementRef;
  @Output() releaseNameClicked = new EventEmitter();
  @Input() lineChartData: Array<any>;
  @Input() lineChartHeight: number;
  @Input() lineChartWidth: number;
  @Input() xlabel;
  @Input() ylabel;
  @Input() chartTitle;
  @Input() chartType; // PhaseWise or ReleaseWise
  target: string;

  isSingleLineChart: boolean;
  margin: any = { top: 10, bottom: 10, left: 30, right: 10 };
  constructor(private coreUtilsService: CoreUtilsService) { }

  ngOnChanges() {
    (function (arr) {
      arr.forEach(function (item) {
        if (item.hasOwnProperty('remove')) {
          return;
        }
        Object.defineProperty(item, 'remove', {
          configurable: true,
          enumerable: true,
          writable: true,
          value: function remove() {
            this.parentNode.removeChild(this);
          }
        });
      });
    })([Element.prototype, CharacterData.prototype, DocumentType.prototype]);
    // this.lineChartHeight =  200;
    // this.lineChartWidth = window.innerWidth;
    this.createLineChart();
  }
  createLineChart() {
    // set the dimensions and margins of the graph
    // this.setHeight();
    const widthChart = this.lineChartWidth > 300 ? this.lineChartWidth / 2 : this.lineChartWidth;
    let width =   (widthChart + 100 - this.margin.left - this.margin.right - 40);
    const height = (this.lineChartHeight - this.margin.top - this.margin.bottom);
    // parse the XAxisPointName / time
    const _this = this;
    // format the data
    this.lineChartData.forEach(function (d) {

      // if (!_this.isSingleLineChart) {
      //   d.Actual = +d.Actual;
      // }
      d.XAxisPointName = d.XAxisPointName;
      d.PlotData = +d.PlotData;
    });
    // set the ranges

    if (this.lineChartData.length > 10) {
      width = width + ((this.lineChartData.length - 10) * 30);
    }

    const x = d3.scalePoint().range([0, width]);
    const y = d3.scaleLinear().range([height, 0]);

    // append the svg obgect to the body of the page
    const element = this.containerLineChart.nativeElement;
    this.deleteElementIfAlready(element);

    // Define the div for the tooltip
    const div = d3.select(element).append('div')
      .attr('class', 'tooltip')
      .style('opacity', 0);

    const svg = d3.select(element).append('svg')
      .attr('width', width + this.margin.left + this.margin.right + 40)
      .attr('height', height + this.margin.top + this.margin.bottom + 70)
      .append('g')
      .attr('transform',
        'translate(' + this.margin.left + ',' + this.margin.top + ')');


        x.domain(this.lineChartData.map( (d) => { return d.XAxisPointName}));

    let yaxisMinValue;
    let yaxisMaxValue;
      yaxisMinValue = d3.min(this.lineChartData, function (d) { return d.PlotData });
      yaxisMaxValue = d3.max(this.lineChartData, function (d) { return d.PlotData });
      y.domain([yaxisMinValue, yaxisMaxValue]).nice();

      if (  yaxisMaxValue - yaxisMinValue < 20 ) {
        const d = this.adjustYAxisGrid(yaxisMinValue, yaxisMaxValue)
        y.domain([d.min, d.max]).nice();
      } 

   
    const color = d3.scaleOrdinal(d3.schemeCategory10);
    color.domain(d3.keys(this.lineChartData[0]).filter(function (key) {
      return key !== 'XAxisPointName' && key !== '_id';
    }));
    const data = this.lineChartData;
    const mappedData = color.domain().map(function (name) {
      return {
        name: name,
        values: data.map(function (d) {
          return {
            XAxisPointName: d.XAxisPointName,
            mapvalue: +d[name]
          };
        })
      };
    });
    // Set the X axis
    svg.append('g')
      .attr('class', 'x axis')
      .attr('transform', 'translate(0,' + height + ')')
      .call(x);

    // Set the Y axis
    svg.append('g')
      .attr('class', 'y axis')
      .call(y)
      .append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 6)
      .attr('dy', '.71em')
      .style('text-anchor', 'end');

    const line = d3.line()
    .curve(d3.curveMonotoneX)
      .x(function (d) {
        return x(d.XAxisPointName);
      })
      .y(function (d) {
        return y(d.mapvalue);
      });


    // Draw the lines
    const svgLineChart = svg.selectAll('.mapped-data')
      .data(mappedData)
      .enter().append('g')
      .attr('class', 'mapped-data');

    svgLineChart.append('path')
      .attr('class', 'line')
      .attr('fill', 'none')
      .attr('d', function (d) {
        return line(d.values);
      })
      .style('stroke', function (d) {
        return '#1E88E5';
      });
    // Add the circles
    const mappValueMethod = _this.mapvalue;
    const setLeft = _this.setLeft;
    const setTop = _this.setTop;
    const cursorCSS = (_this.chartType === 'PhaseWise') ? 'pointer' : 'default';
    svgLineChart.append('g').selectAll('circle')
      .data(function (d) { return d.values; })
      .enter()
      .append('circle')
      .style('cursor', cursorCSS)
      .attr('r', 4)
      .attr('cx', function (dd) { return x(dd.XAxisPointName); })
      .attr('cy', function (dd) { return y(dd.mapvalue); })
      .attr('fill', function (d) {

     
        if (_this.isSingleLineChart) {
          return '#ff7f0e';
        } else {
          // Fixed 716247 - Changed the color code
          return (color(this.parentNode.__data__.name) === '#ff7f0e')
            ? '#1E88E5' : color(this.parentNode.__data__.name);
        }
      })
      .attr('stroke', function (d) {
       
        if (_this.isSingleLineChart) {
          return '#ff7f0e';
        } else {
          return (color(this.parentNode.__data__.name) === '#ff7f0e')
            ? '#1E88E5' : color(this.parentNode.__data__.name);
        }
      })
      .on('click', function (d) {
        // alert('clicked phase'+ d.XAxisPointName);
        if ( _this.chartType === 'PhaseWise') {
           _this.releaseNameClicked.emit([d.XAxisPointName, _this.chartTitle]);
        }
      })
      .on('mouseover', function (d) {
        div.transition()
          .duration(200)
          .style('opacity', .9);
        div.html(mappValueMethod(d.XAxisPointName, d, _this.lineChartData, _this))
          .style('left', _this.setLeft(d3.event))
          .style('top', _this.setTop(d3.event));
      })
      .on('mouseout', function (d) {
        div.transition()
          .duration(500)
          .style('opacity', 0);
      });




    // Add the X Axis

    const xaxisdraw = svgLineChart.append('g')
      .attr('class', 'x-axis-text')
      .attr('transform', 'translate(0,' + height + ')');
        // added tickValues to remove the duplicate dates
        const tickValuesForAxis = data.map(d => d.XAxisPointName);
        xaxisdraw
          .call(d3.axisBottom(x).tickValues(tickValuesForAxis));
    xaxisdraw
      .selectAll('text')
      .style('text-anchor', 'end')
      .style('font', '9px sans-serif')
      .attr('transform', 'rotate(-60)');
    
    // Add the Y Axis
    svgLineChart.append('g')
      .attr('class', 'y-axis-text')
      .call(d3.axisLeft(y));
    svgLineChart.attr('transform', 'translate(' + this.margin.left + ',' + this.margin.top + ')');

    svgLineChart.append('text')
    .attr('transform', 'rotate(-90)')
    .attr('y', 0 - (this.margin.left + 30))
    .attr('x', 0 - (height / 2))
    .attr('dy', '1em')
    .style('text-anchor', 'middle')
    .style('font', '14px sans-serif')
    .text(this.ylabel);

    svgLineChart.append('text')
    .attr('transform',
      'translate(' + (width / 2) + ' ,' +
      (height + this.margin.top + 50) + ')')
    .style('text-anchor', 'middle')
    .style('font', '14px sans-serif')
    .text(this.xlabel);

    const wrapText1 = _this.wrapWord;
    svgLineChart.selectAll('.tick text')
      .call(wrapText1, 20);

      const wrapText = _this.wrapWordEllipse;
      svgLineChart.selectAll('.y-axis-text text')
        .call(wrapText, 30);
      const wrapTextX = _this.wrapWordEllipseXTickLabel;
      svgLineChart.selectAll('.x-axis-text text')
        .call(wrapTextX, 30);
        
  }
  setHeight() {
    let barHeight = 0;
    let barWidth = 0;
    for (let index = 1; index <= this.lineChartData.length; index++) {
      if (index % 10 === 0) {
        barHeight = barHeight + 5;
        barWidth = this.lineChartData.length > 60 ? barWidth + 150 : barWidth + 15;
        barWidth = this.lineChartData.length > 600 ? barWidth + 160 : barWidth;
      }
    }
    this.lineChartHeight = this.lineChartHeight + barHeight;
    this.lineChartWidth = this.lineChartWidth + barWidth;
  }

  formatDate(date: any) {
    const dateTime = new Date(date);
    date = _moment.utc(dateTime);
    return date ? date : null;
  }
  mapvalue( xname, d: any, data: any, _this) {
    const XAxisPointName = data.filter(x => x.XAxisPointName === d.XAxisPointName);
    let name = '';
    // const actualFlilter = filterByDate.filter(value => value.Actual === d.mapvalue);
    const forCastFlilter = XAxisPointName.filter(value => value.PlotData === d.mapvalue);
    let bckgroundColorClass = '';

    if (forCastFlilter.length > 0) {
      name = '<b> Value : </b>';
      bckgroundColorClass = 'forecast-bg-color';
    }

    const mappedData = d.mapvalue;
    const innerHTML = '<div class=' + bckgroundColorClass + '><div><b> '+ _this.chartTitle +' </b></div><div><b> ' + xname + '</b></div><div>' + name
      + ' ' + mappedData + '</div></div>';
    return innerHTML;
  }
  setLeft(event) {
    let left;
      left = event.layerX + 'px';
    return left;
  }
  setTop(event) {
    let top;
      top = event.layerY + 'px';
    return top;
  }
  wrapWord(text, width) {
    text.each(function () {
      // tslint:disable: no-shadowed-variable
      const text = d3.select(this);
      const words = text.text().split(/\s+/).reverse();
      if (words.length > 2) {
        let word;
        let line = [],
          lineNumber = 0;
        const lineHeight = 1.1,
          y = text.attr('y'),
          dy = parseFloat(text.attr('dy'));
        let tspan = text.text(null).append('tspan').attr('x', 0).attr('y', y).attr('dy', dy + 'em');
        while (word = words.pop()) {
          line.push(word);
          tspan.text(line.join(' '));
          if (tspan.node().getComputedTextLength() > width) {
            line.pop();
            tspan.text(line.join(' '));
            line = [word];
            tspan = text.append('tspan').attr('x', 0).attr('y', y).attr('dy', ++lineNumber * lineHeight + dy + 'em').text(word);
          }
        }
      }
    });
  }

  wrapWordEllipse(label, width) {
    label.each(function () {
      // const self = d3.select(this);
      // let textLength = label.length;
      // let text = self.text();
      let text = d3.select(this);
      text = text._groups[0][0];
      if (text.textContent.length > 5) {
        text.textContent = text.textContent.substring(0, 5) + '....';
      }
    });

  }

  wrapWordEllipseXTickLabel(label, width) {
    label.each(function () {
      // const self = d3.select(this);
      // let textLength = label.length;
      // let text = self.text();
      let text = d3.select(this);
      text = text._groups[0][0];
      if (text.textContent.length > 10) {
        text.textContent = text.textContent.substring(0, 10) + '';
      }
    });

  }

  deleteElementIfAlready(element: any) {
    if (element.hasChildNodes()) {
      let eleLength = element.childNodes.length;
      do {
        const childNodes = Array.from(element.childNodes);

        Array.prototype.forEach.call(childNodes, function (childNode) {
          childNode.remove();
        });

        eleLength = element.childNodes.length;
      } while (eleLength > 0);
    }
  }

  adjustYAxisGrid(yaxisMinValue, yaxisMaxValue) {
    const d = {
      min: yaxisMinValue,
      max: yaxisMaxValue
    };
    if (d.min > 0) {
      d.min = 0;
    } else {
      d.min = d.min * 2;
    }

    d.max = (Math.round(d.max));
    d.max = d.max * (3);

    return d;
  }



}
