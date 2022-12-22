import { Component, OnInit, ElementRef, ViewChild, Input, OnChanges } from '@angular/core';
import * as d3 from 'd3';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import * as moment from 'moment';
import * as _ from 'lodash';
const _moment = moment;

@Component({
  selector: 'app-line-chart-monitoring',
  templateUrl: './line-chart-monitoring.component.html',
  styleUrls: ['./line-chart-monitoring.component.scss']
})
export class LineChartMonitoringComponent implements OnChanges {
  @ViewChild('containerLineChart', { static: true }) containerLineChart: ElementRef;
  @Input() lineChartData: Array<any>;
  @Input() lineChartHeight: number;
  @Input() lineChartWidth: number;
  @Input() pageInfo: any;
  @Input() monitoring: boolean;
  @Input() entityName; // accuracy, dataquality , inputdrift , targetvariance
  @Input() thersholdValue;
  target: string;
  name;

  isSingleLineChart: boolean;
  margin: any = { top: 10, bottom: 20, left: 30, right: 20 };
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
    this.lineChartHeight = this.monitoring ? this.lineChartHeight : 350;
    this.lineChartWidth = this.monitoring ? this.lineChartWidth : window.innerWidth;
    this.createLineChart();
  }
  createLineChart() {
    // set the dimensions and margins of the graph
    // this.setHeight();
    const widthChart = this.lineChartWidth > 300 ? this.lineChartWidth / 2 : this.lineChartWidth;
    let width = this.monitoring ? this.lineChartWidth : (widthChart + 100 - this.margin.left - this.margin.right - 40);
    const height = this.monitoring ? this.lineChartHeight : (this.lineChartHeight - this.margin.top - this.margin.bottom);
    // parse the RangeTime / time

    const parseTime = d3.timeParse('%d/%m/%Y');
    const parseUTCDate = d3.utcParse('%Y-%m-%dT%H:%M:%S.%LZ');
    const formatUTCDate = d3.timeFormat('%Y-%m-%d');
    const parseDate = d3.timeParse('%Y-%m-%d %H:%M:%S');
    const formatDate = d3.timeFormat('%b-%Y');
    const formatTime = d3.timeFormat('%Y-%m-%d %X');

    const _this = this;
    // format the data
    this.lineChartData.forEach(function (d) {
      const tt = parseUTCDate(d.RangeTime);
      const dd = formatUTCDate(d.RangeTime);
      const ll = formatDate(d.rangeTime);

      if (!_this.coreUtilsService.isNil(d.RangeTime)) {

        const ddd = _this.formatDate(d.RangeTime.split(' ')[0]);
        d.RangeTime = ddd._d;
      }

      if (!_this.isSingleLineChart) {
        d.Actual = +d.Actual;
      }
      d.Forecast = +d.Forecast;
    });

    if (this.lineChartData.length > 10) {
      width = width + ((this.lineChartData.length - 10) * 30);
    }

    const x = d3.scaleLinear().range([0, width]);
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

    // sort years ascending
    this.lineChartData.sort(function (a, b) {
      return a['RangeTime'] - b['RangeTime'];
    });

    x.domain(d3.extent(this.lineChartData, function (d) { return d.RangeTime; }));

    let yaxisMinValue;
    let yaxisMaxValue;
    yaxisMinValue = d3.min(this.lineChartData, function (d) { return Math.min(d.Actual, d.Forecast); });
    yaxisMaxValue = d3.max(this.lineChartData, function (d) { return Math.max(d.Actual, d.Forecast); });
    y.domain([yaxisMinValue, yaxisMaxValue]).nice();


    const color = d3.scaleOrdinal(d3.schemeCategory10);
    color.domain(d3.keys(this.lineChartData[0]).filter(function (key) {
      return key !== 'RangeTime' && key !== '_id';
    }));
    const data = this.lineChartData;
    const mappedData = color.domain().map(function (name) {
      return {
        name: name,
        values: data.map(function (d) {
          return {
            rangeTime: d.RangeTime,
            mapvalue: +d[name],
            name: name,
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
        return x(d.rangeTime);
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
      .attr("stroke-width",2)
      .style('stroke', function (d) {
        let color = '';
        if (_this.entityName === 'accuracy') color = '#9657D5';
        if (_this.entityName === 'inputdrift') color = '#EDB0E6';
        if (_this.entityName === 'targetvariance') color = '#6D6ACC';
        if (_this.entityName === 'dataquality') color = '#87C6E2';
        return d.name === 'Actual' ? '#60607F' : color;
      });
    // Add the circles
    const mappValueMethod = _this.mapvalue;
    const setLeft = _this.setLeft;
    const setTop = _this.setTop;
  
    svgLineChart.append('g').selectAll('circle')
      .data(function (d) { 
        return d.values;
       })
      .enter()
      .append('circle')
      .attr('r', 5)
      .attr('cx', function (dd) { 
        return x(dd.rangeTime);
       })
      .attr('cy', function (dd) { return y(dd.mapvalue); })
      .attr('fill', function (d) {
        if (_this.entityName === 'accuracy' || _this.entityName === 'dataquality') {
          if (d.mapvalue < _this.thersholdValue) {
            return '#db5e5a';
          }
        }
        if (_this.entityName === 'targetvariance' || _this.entityName === 'inputdrift') {
          if (d.mapvalue > _this.thersholdValue) {
            return '#db5e5a';
          }
        }
        let color = '';
        if (_this.entityName === 'accuracy') color = '#9657D5';
        if (_this.entityName === 'inputdrift') color = '#EDB0E6';
        if (_this.entityName === 'targetvariance') color = '#6D6ACC';
        if (_this.entityName === 'dataquality') color = '#87C6E2';
        // return '#60607F';
        return d.name === 'Actual' ? '#60607F' : color;
      })
      // .attr('stroke', function (d) {
      //   if (_this.entityName === 'accuracy' || _this.entityName === 'dataquality') {
      //     if (d.mapvalue < _this.thersholdValue) {
      //       return '#db5e5a';
      //     }
      //   }
      //   if (_this.entityName === 'targetvariance' || _this.entityName === 'inputdrift') {
      //     if (d.mapvalue > _this.thersholdValue) {
      //       return '#db5e5a';
      //     }
      //   }
      //   let color = '';
      //   if (_this.entityName === 'accuracy') {
      //     color = '#9657D5';
      //   }
      //   if (_this.entityName === 'inputdrift') {
      //     color = '#EDB0E6';
      //   }
      //   if (_this.entityName === 'targetvariance') {
      //     color = '#6D6ACC';
      //   }
      //   if (_this.entityName === 'dataquality') {
      //     color = '#87C6E2';
      //   }
      //   return d.name === 'Actual' ? '#60607F' : color;
      // })
      .on('mouseover', function (d) {
        div.transition()
          .duration(200)
          .style('opacity', .9);
        div.html(mappValueMethod(formatTime(d.rangeTime), d, _this.lineChartData, _this))
          .style('left', _this.setLeft(d3.event))
          .style('top', _this.setTop(d3.event));
      })
      .on('mouseout', function (d) {
        div.transition()
          .duration(500)
          .style('opacity', 0);
      });


    let fomratStr;
    fomratStr = '%Y-%b-%d';
    // Add the X Axis
    const xaxisdraw = svgLineChart.append('g')
      .attr('transform', 'translate(0,' + height + ')');
    // added tickValues to remove the duplicate dates
    const tickValuesForAxis = data.map(d => d.RangeTime);
    xaxisdraw
      .call(d3.axisBottom(x).tickValues(tickValuesForAxis).tickFormat(d3.timeFormat(fomratStr)));
    xaxisdraw
      .selectAll('text')
      .style('text-anchor', 'end')
      .attr('transform', 'rotate(-65)');

    // end of x axis

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
      .text('Percentage Change');

    const wrapText1 = _this.wrapWord;
    svgLineChart.selectAll('.tick text')
      .call(wrapText1, 80);

    const wrapText = _this.wrapWordEllipse;
    svgLineChart.selectAll('.y-axis-text text')
      .call(wrapText, 30);
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
  mapvalue(formattedTime, d: any, data: any, _this) {
    const filterByDate = data.filter(x => x.RangeTime === d.rangeTime);
    let name = '';
    const actualFlilter = filterByDate.filter(value => value.Actual === d.mapvalue);
    const forCastFlilter = filterByDate.filter(value => value.Forecast === d.mapvalue);
    let bckgroundColorClass = '';
    if (actualFlilter.length > 0) {
      name = '<b> Threshold : </b>';
      bckgroundColorClass = 'actual-bg-color';
    }
    if (forCastFlilter.length > 0) {
      name = '<b> Current : </b>';
      bckgroundColorClass = 'forecast-bg-color';
    }

    if (_this.entityName === 'accuracy' || _this.entityName === 'dataquality') {
      if (d.mapvalue < _this.thersholdValue) {
        bckgroundColorClass = 'red-bg-color';
      }
    }
    if (_this.entityName === 'targetvariance' || _this.entityName === 'inputdrift') {
      if (d.mapvalue > _this.thersholdValue) {
        bckgroundColorClass = 'red-bg-color';
      }
    }
    const mappedData = d.mapvalue;
    const innerHTML = '<div class=' + bckgroundColorClass + '><div><b>Date : </b>' + formattedTime + '</div><div>' + name
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
    d.max = d.max * (1.5);

    return d;
  }



}
