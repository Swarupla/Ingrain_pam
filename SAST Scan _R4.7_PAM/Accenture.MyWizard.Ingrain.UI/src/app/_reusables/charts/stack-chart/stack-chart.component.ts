
import { Component, Input, ElementRef, OnInit, ViewChild, OnChanges, SimpleChanges, SimpleChange } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-stack-chart',
  templateUrl: './stack-chart.component.html',
  styleUrls: ['./stack-chart.component.scss']
})
export class StackChartComponent implements OnInit {
  @ViewChild('containerLineChart', { static: true }) containerLineChart: ElementRef;
  @Input() data;
  @Input() ylabels;
  @Input() visualizationResponse;
  @Input() modelMonitoring;
  private w = 280;
  private h = 260;
  private margin = { top: 10, right: 10, bottom: 40, left: 30 };
  private width = this.w - this.margin.left - this.margin.right;
  private height = this.h - this.margin.top - this.margin.bottom;

  private x: any;
  private y: any;
  private svg: any;
  private g: any;
  private stack: any;
  private chart: any;
  private layersBarArea: any;
  private div1;
  private layersBar: any;
  private xAxis: any;
  private yAxis: any;
  private legend: any;
  private legendItems: any;
  private tooltip: any;
  private stackedSeries: any;
  public pageNumber = 1;
  public maxLimit = 15;
  public dataCopy ;
  public fullTextTooltip: any;

  private colors = ['rgb(38, 58, 127)', 'rgb(47, 151, 212)', 'rgb(72, 199, 240)',
  'rgb(173, 199, 12)', 'rgb(38, 158, 127)', 'rgb(47, 151, 212)', 'rgb(72, 199, 240)', 'rgb(173, 199, 12)',
  'rgb(38, 58, 127)', 'rgb(47, 151, 212)', 'rgb(72, 199, 240)', 'rgb(173, 199, 12)'];
  target: any;

  constructor(private container: ElementRef) {
  }

  ngOnInit(noset?) {
      // append the svg obgect to the body of the page
      if (noset === undefined) { this.dataCopy = this.data;}
      const element = this.containerLineChart.nativeElement;
      this.deleteElementIfAlready(element);

     if ( this.modelMonitoring) {
      this.w = window.innerWidth - 100;
      // if (this.data.length > 15) {
      //   this.w = this.w + ( (this.data.length -15) * 50);
      // }
      this.h = 500; 
      // if ( this.ylabels.length > 3) {
      //   this.margin.bottom =  this.margin.bottom + (this.ylabels.length - 3 ) * 15;
      // }
     this.width = this.w - this.margin.left - this.margin.right;
     this.height = this.h - this.margin.top - this.margin.bottom;
     }

     const xAxisText = d3.select(element).append('div')
      .attr('id', 'xFullTextStack')
      // .style('display', 'none')
      .style('opacity', 0)
      .attr('class', 'tooltip-stack')
      .style('font-size', '0.7rem');

    this.target = this.visualizationResponse.target ? this.visualizationResponse.target : this.visualizationResponse.Target;
    this.stack = d3.stack()
      .keys(this.ylabels);
      
    this.data.sort((a, b) => (a.xlabel > b.xlabel) ? 1 : ((b.xlabel > a.xlabel) ? -1 : 0));
    this.data = this.dataCopy.slice((this.pageNumber - 1)* this.maxLimit,this.pageNumber * this.maxLimit);
    this.initScales();
    this.initSvg();
    this.createStack(this.data);
    this.drawAxis();
  }

  // 	ngOnChanges(changes: SimpleChanges) {
  // 		const dataChange = changes.data;
  // 		if(dataChange.firstChange === false){
  // 			this.stack = d3.stack()
  // 				.keys(this.keys);
  // 			this.createStack(this.data);
  // 		}
  // 	}

  private initScales() {
    this.x = d3.scaleBand()
      .rangeRound([0, this.width])
      .padding(0.2);

    this.y = d3.scaleLinear()
      .range([this.height, 0]);
  }

  private initSvg() {

    const viewBoxValue = (this.modelMonitoring) ? '0 0 700 500' : '0 0 280 260'
    this.svg = d3.select(this.container.nativeElement)
      .select('.chart-container')
      .append('svg')
      .attr('preserveAspectRatio', 'xMinYMin meet')
      .attr('class', 'chart')
      .attr('width', this.w)
      .attr('height', this.h)
      .attr('viewBox', viewBoxValue);

        // Define the div for the tooltip

    this.chart = this.svg.append('g')
      .classed('chart-contents', true)
      .attr('transform', 'translate(' + (this.margin.left + 10) + ',' + this.margin.top + ')');

    this.layersBarArea = this.chart.append('g')
      .classed('layers', true);
  }

  private drawAxis() {
    this.xAxis = this.chart.append('g')
      .classed('x-axis', true)
      .attr('transform', 'translate(0,' + (this.height) + ')')
      .call(d3.axisBottom(this.x));



    this.chart.append('text')
      .attr('y', this.height + 40)
      .attr('x', (this.width / 2))
      .classed('axis-title', true)
      .style('text-anchor', 'middle')
      .style('stroke', 'none');
    // .text(this.xTitle);

    this.chart
      .selectAll('.x-axis text')
      .on('mouseover', (d, index, elements) => {
        if (typeof d == 'number') {
          d = d.toString();
        }

        if (d.length > 4) {
          let event = d3.event;
          let coords = [event.offsetX, event.offsetY + 40];
          this.showFullText(d, event, coords);
        }
      })
      .on('mouseleave', d => {
        if (typeof d == 'number') {
          d = d.toString();
        }

        if (d.length > 4) {
          this.hideTooltip(d);
        }
      });

      const _this = this;
      const wrapText = this.wrapWordEllipse;
      this.chart.selectAll('g.x-axis text')
      .call(wrapText, 30);

    this.chart.append('text')
      .attr('transform',
        'translate(' + (this.width / 2 - 90) + ' ,' +
        (this.height + this.margin.top + 25) + ')')
      .style('text-anchor', 'start')
      .style('font', '12px sans-serif')
      .text(this.visualizationResponse.xlabelname);

    this.chart.append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - (this.margin.left + 13))
      .attr('x', 0 - (this.height / 2))
      .attr('dy', '1em')
      .style('text-anchor', 'middle')
      .style('font', '14px sans-serif')
      .text(this.visualizationResponse.ylabelname);

    // if ( this.modelMonitoring) {
    //   const legend = this.chart.append('g')
    //   .attr('class', 'legendLinear')
    //   .attr('transform', 'translate(-30, ' + (this.height + 25) + ')');

    // legend.selectAll('rect')
    //   .data(this.ylabels)
    //   .enter()
    //   .append('rect')
    //   .attr('x', 0 + this.margin.left)
    //   .attr('y', (d, i) => {
    //     return i * 15;
    //   })
    //   .attr('width', 12)
    //   .attr('height', 12)
    //   .attr('fill', (d: any, i: any) => {
    //     return this.colors[i];
    //   });

    // legend.selectAll('text')
    //   .data(this.ylabels)
    //   .enter()
    //   .append('text')
    //   .text(function (d) {
    //     return d;
    //   })
    //   .attr('x', 15 + this.margin.left)
    //   .attr('y', (d, i) => {
    //     return i * 15;
    //   })
    //   .attr('text-anchor', 'start')
    //   .attr('alignment-baseline', 'hanging'); 
    // }
    // legend.selectAll('rect')
    //   .data(this.ylabels)
    //   .enter()
    //   .append('rect')
    //   .attr('x', function (d, i) {
    //     return i * 40;
    //   })
    //   .attr('y', 12)
    //   .attr('width', 12)
    //   .attr('height', 12)
    //   .attr('fill', (d: any, i: any) => {
    //     return this.colors[i];
    //   });

    // legend.selectAll('text')
    //   .data(this.ylabels)
    //   .enter()
    //   .append('text')
    //   .text(function (d) {
    //     return d;
    //   })
    //   .attr('x', function (d, i) {
    //     return (i * 2 + 1) * 20;
    //   })
    //   .attr('y', 12)
    //   .attr('text-anchor', 'start')
    //   .attr('alignment-baseline', 'hanging');

    this.yAxis = this.chart.append('g')
      .classed('y axis', true)
      .call(d3.axisLeft(this.y)
        .ticks(7));

    this.chart.append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - 60)
      .attr('x', 0 - (this.height / 2))
      .style('text-anchor', 'middle')
      .style('stroke', 'none')
      .classed('axis-title', true);
    // .text(this.yTitle);
  }

  wrapWordEllipse(label, width) {
    label.each(function () {
      // const self = d3.select(this);
      // let textLength = label.length;
      // let text = self.text();
      let text = d3.select(this);
      text = text._groups[0][0];
      if (text.textContent.length > 4) {
        text.textContent = text.textContent.substring(0, 4) + '...';
      }
    });

  }

  showFullText(d, event, coords) {
    const fullTextTooltip = d3.select('#xFullTextStack');
    fullTextTooltip.transition().duration(100).style('opacity', 1);
    fullTextTooltip.html(d);
    // this.fullTextTooltip.style('display', 'block');
    
    fullTextTooltip.style('left', coords[0] + 'px');
    fullTextTooltip.style('top', coords[1] + 'px');
  }

  hideTooltip(d) {
    const fullTextTooltip = d3.select('#xFullTextStack');
    // this.fullTextTooltip.style('display', 'none');
    fullTextTooltip.transition().duration(100).style('opacity', 0);
  }

  private createStack(stackData: any) {
    this.stackedSeries = this.stack(stackData);
    // console.log(this.stackedSeries)
    this.drawChart(this.stackedSeries);
  }

  private drawChart(data: any) {
    const _this = this;
    const div1 = d3.select(this.container.nativeElement).append('div')
    .attr('class', 'tooltip-stack')
    .style('opacity', 0);

    this.layersBar = this.layersBarArea.selectAll('.layer')
      .data(data)
      .enter()
      .append('g')
      .classed('layer', true)
      .style('fill', (d: any, i: any) => {
        console.log(d);
        return this.colors[i];
      });
      // .on('mouseover', function (d,e) {
      //   div1.transition()
      //     .duration(400)
      //     .style('opacity', .9);
      //   div1.html('<div class= text-align><b> ' + d.key + ' : ' + d[d.index].data[d.key] + '  </b></div>')
      //   .style('left', _this.setLeft(d3.event))
      //   .style('top', _this.setTop(d3.event));
      // })
      // .on('mouseout', function (d) {
      //   div1.transition()
      //     .duration(500)
      //     .style('opacity', 0);
      // })
    this.x.domain(this.data.map((d: any) => {
      return d.xlabel;
    }));

    this.y.domain([0, +d3.max(this.stackedSeries, function (d: any) {
      return d3.max(d, (d1: any) => {
        return d1[1];
      });
    })]);

    this.layersBar.selectAll('rect')
      .data((d: any) => {
        return d;
      })
      .enter()
      .append('rect')
      .attr('y', (d: any) => {
        return _this.y(d[1]);
      })
      .attr('x', (d: any, i: any) => {
        return _this.x(d.data.xlabel);
      })
      .on('mouseover', function (d,e) {
        div1.transition()
          .duration(400)
          .style('opacity', .9);
        div1.html('<div class= text-align><b> ' + _this.getCurrentTooltipValue(d) + '  </b></div>')
        .style('left', _this.setLeft(d3.event))
        .style('top', _this.setTop(d3.event));
      })
      .on('mouseout', function (d) {
        div1.transition()
          .duration(500)
          .style('opacity', 0);
      })
      .attr('width', this.x.bandwidth())
      .attr('height', (d: any, i: any) => {
        let sub = 0;
        if ( (_this.y(d[0]) - _this.y(d[1])) < 0) {
          sub = (_this.y(d[1]) - _this.y(d[0]))
        } else {
          sub = (_this.y(d[0]) - _this.y(d[1]))
        }
        return sub;
      });
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

  getCurrentTooltipValue(d) {
    console.log(d.data); // full bar d[0] previous d[1] current
    let returnString = '';
    const keys = Object.keys(d.data);
    let total = 0;
    for ( let i = 0 ; i < keys.length ; i++) {
      total += d.data[keys[i]];
      if ( total === d[1]) {
        returnString = keys[i] + ' : ' + d.data[keys[i]];
      }
    }
   return returnString;
  }


  nextData(){

   this.data = this.dataCopy.slice((this.pageNumber - 1)* this.maxLimit,this.pageNumber * this.maxLimit);
  //  this.pageNumber++;
   this.ngOnInit('noset');
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

  changePage($event) {
    this.pageNumber = $event;
    this.nextData();
  }
}
