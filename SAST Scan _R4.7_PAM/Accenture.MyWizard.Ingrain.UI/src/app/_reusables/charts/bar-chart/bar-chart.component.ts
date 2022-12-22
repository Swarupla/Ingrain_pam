import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, Input, OnChanges } from '@angular/core';
import * as d3 from 'd3';

const DEFAULT_CLUSTER_COLORS = {
  'Cluster0': '#0000FF',
  'Cluster1': '#FF0000',
  'Cluster2': '#008000',
  'Cluster3': '#000000',
  'Cluster4': '#808080',
  'Cluster5': '#800000',
  'Cluster6': '#FFFF00',
  'Cluster7': '#808000',
  'Cluster8': '#00FF00',
  'Cluster9': '#00FFFF',
  'Cluster10': '#008080',
  'Cluster11': '#000080',
  'Cluster12': '#FF00FF',
  'Cluster13': '#C0C0C0',
  'Cluster14': '#800080'
};

@Component({
  selector: 'app-bar-chart',
  templateUrl: './bar-chart.component.html',
  styleUrls: ['./bar-chart.component.scss']
})
export class BarChartComponent implements OnInit, AfterViewInit, OnChanges {
  @ViewChild('containerBarChart', { static: true }) containerBarChart: ElementRef;
  htmlElement: HTMLElement;
  host;
  svg;
  @Input() barchartHeight: number;
  @Input() barchartWidth: number;
  @Input() modeltype: string;
  @Input() data: Array<any>;
  margin: any = { top: 20, bottom: 40, left: 30, right: 20 };

  constructor() { }

  ngOnInit() { }

  ngOnChanges(changes) {
    this.createChart();
  }

  ngAfterViewInit() {
    this.data = this.data = this.data.sort((a, b) => (a.prcntvalue > b.prcntvalue) ? 1 : ((b.prcntvalue > a.prcntvalue) ? -1 : 0));
    this.createChart();
  }

  createChart() {
    const oldWidth = this.barchartWidth;
    const oldHeight = this.barchartHeight;
     
    const _this = this;
    const width = oldWidth - this.margin.left - this.margin.right;
    let height = oldHeight - this.margin.top - this.margin.bottom;
    let model = this.modeltype;
    if (this.data.length > 15) {
      height = height + (this.data.length * 20);
    }

    const y = d3.scaleBand()
      .range([height, 0])
      .padding(0.5);

    const x = d3.scaleLinear()
      .range([0, width]);

    const element = this.containerBarChart.nativeElement;
    element.innerText = '';
    const div = d3.select(element).append('div')
      .attr('class', 'tooltip')
      .style('opacity', 0);
    const svgEle = d3.select(element).append('svg');
    const svg = svgEle
      .attr('width', width + this.margin.left + this.margin.right)
      .attr('height', height + this.margin.top + this.margin.bottom)
      .append('g')
      .attr('transform',
        'translate(' + this.margin.left + ',' + this.margin.top + ')');

    this.data.forEach(function (d) {
      d.prcntvalue = +d.prcntvalue;
    });
    const minValue = d3.min(this.data, function (d) { return d.prcntvalue; });

    if (minValue >= 0) {
      // Scale the range of the data in the domains
      x.domain([0, d3.max(this.data, function (d) { return d.prcntvalue; })]).nice();
      y.domain(this.data.map(function (d) { return d.featurename; }));

      let sequentialScale = d3.scaleSequential()
      .domain(d3.extent(this.data, function (d) { return d.prcntvalue; }))
      .interpolator(d3.interpolateRainbow);

      // gridlines in x axis function
      const xAxisGrid = d3.axisBottom(x).tickSize(-height).tickFormat('');

      // add the X gridlines
      svg.append('g')
      .attr('class', 'x xaxis-grid')
      .attr('transform', 'translate(0,' + height + ')')
      .call(xAxisGrid)
      .selectAll('.tick')
      .attr('stroke', 'lightblue')
      .attr('opacity', '0.2');

      const rx = y.bandwidth() / 2;
      const ry = y.bandwidth() / 2;

      // append the rectangles for the bar chart
      svg.selectAll('.bar')
        .data(this.data)
        /*.enter().append('rect')
        .attr('width', function (d) { return x(d.prcntvalue); })
        .attr('y', function (d) { return y(d.featurename); })
        .attr('height', y.bandwidth()) */
        .enter().append('path')
        .attr('class', 'rect-path')
        .attr('d', function (d) {
          console.log(d.featurename+d.prcntvalue);
          if (d.prcntvalue == 0) {
            return `
                M0,${y(d.featurename)}
                h0
                a${rx},${ry} 0 0 1 ${rx},${ry}
                v0
                a${rx},${ry} 0 0 1 ${-rx},${ry}
                h0Z
              `;
          } else if(d.prcntvalue > 0) {
            if((x(d.prcntvalue)-rx) < 0) {
              return `
                M0,${y(d.featurename)}
                h0
                a${rx},${ry} 0 0 1 ${rx},${ry}
                v0
                a${rx},${ry} 0 0 1 ${-rx},${ry}
                h0Z
              `;
            } else {
              return `
                M0,${y(d.featurename)}
                h${x(d.prcntvalue)-rx}
                a${rx},${ry} 0 0 1 ${rx},${ry}
                v0
                a${rx},${ry} 0 0 1 ${-rx},${ry}
                h${-(x(d.prcntvalue)-rx)}Z
              `;
            }
          } else {
            if((-(x(d.prcntvalue)-rx)) > 0) {
              return `
                M0,${y(d.featurename)}
                h${-(x(d.prcntvalue)-rx)}
                a${rx},${ry} 1 0 0 ${-rx},${ry}
                v0
                a${rx},${ry} 1 0 0 ${rx},${ry}
                h${(x(d.prcntvalue)-rx)}Z
              `;
            } else {
              return `
                M0,${y(d.featurename)}
                h${-(x(d.prcntvalue)-rx)}
                a${rx},${ry} 1 0 0 ${-rx},${ry}
                v0
                a${rx},${ry} 1 0 0 ${rx},${ry}
                h${(x(d.prcntvalue)-rx)}Z
              `;
            }
          }
        })
        .attr('fill', function (d,i) {
          if (d.featurename.includes('Cluster')) {
            return _this.setClusterBarColor(d.featurename);
          } else {
           // return d['color'];
            return sequentialScale(i*10);
          }
        })

      // add a value label to the right of each bar
      svg.selectAll('.bar').data(this.data)
      .enter()
      .append('text')
      .attr('class', 'barvalue')
      // y position of the label is halfway down the bar
      .attr('y', function (d) {
        return y(d.featurename);
      })
      // x position is 3 pixels to the right of the bar
      .attr('x', function (d) {
        return x(Math.min(0, d.prcntvalue)) + 3;
      })
      .attr('font-size', 12)
      .attr('font-family', 'sans-serif')
      .text(function (d) {
        let value = d.prcntvalue.toString()
        if ( value.length > 5) {
        value = value.substring(0,6);
        }
        return value;
      });  

      // add the x Axis
      svg.append('g')
        .attr('class','x-Axis')
        .attr('color', 'grey')
        .attr('transform', 'translate(0,' + height + ')')
        .call(d3.axisBottom(x));

      svg.selectAll('text')
        .style('text-anchor', 'end')
        .attr('transform', 'rotate(-65)');

      svg.selectAll('.barvalue')
        .style('text-anchor', 'start !important')
        .attr('transform', 'rotate(0) !important');

      let maxw = 0;
      // add the y Axis
      svg.append('g')
        .attr('class','y-Axis')
        .attr('color', 'grey')
        .text('Features')
        .attr('transform', 'translate(0,0)')
        .call(d3.axisLeft(y).tickSizeOuter(10))
        .selectAll('text').each(function () {
          if (this.getBBox().width > maxw) {
            maxw = this.getBBox().width;
          }
        });

      svg.selectAll('.rect-path')
        .on('mouseover', function (d) {
          div.transition()
            .duration(400)
            .style('opacity', .9);
          div.html('<div class= text-align><b>Feature : </b>' + d.featurename + '</div><div class=text-align><b>Weight : </b>' +
            d.prcntvalue + '</div>')
            .style('left', '30px')
            .style('top', '30px');
        })
        .on('mouseout', function (d) {
          div.transition()
            .duration(500)
            .style('opacity', 0);
        });

      d3.selectAll('g.x-Axis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.7');
      d3.selectAll('g.y-Axis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.7');
      d3.selectAll('g.x-Axis').selectAll('.tick line').remove();
      d3.selectAll('g.y-Axis').selectAll('.tick line').remove();
      d3.selectAll('g.xaxis-grid').select('path').remove();
      d3.selectAll('g.yaxis-grid').select('path').remove();

    } else {
      x.domain(d3.extent(this.data, function (d) { return d.prcntvalue; })).nice();
      y.domain(this.data.map(function (d) { return d.featurename; }));

      let sequentialScale = d3.scaleSequential()
      .domain(d3.extent(this.data, function (d) { return d.prcntvalue; }))
      .interpolator(d3.interpolateRainbow);

      const rx = y.bandwidth() / 2;
      const ry = y.bandwidth() / 2;

      // gridlines in x axis function
      const xAxisGrid = d3.axisBottom(x).tickSize(-height).tickFormat('');

      // add the X gridlines
      svg.append('g')
      .attr('class', 'x xaxis-grid')
      .attr('transform', 'translate(0,' + height + ')')
      .call(xAxisGrid)
      .selectAll('.tick')
      .attr('stroke', 'lightblue')
      .attr('opacity', '0.2');

     
      svg.selectAll('.bar')
        .data(this.data)
        /* .enter().append('rect')
        .attr('x', function (d) { return x(Math.min(0, d.prcntvalue)); })
        .attr('width', function (d) { return Math.abs(x(d.prcntvalue) - x(0)); })
        .attr('y', function (d) { return y(d.featurename); })
        .attr('height', y.bandwidth()) */
        .enter().append('path')
        .attr('class', 'rect-path')
        .attr('d', function (d) {
          // console.log(d.featurename+d.prcntvalue);
          if (d.prcntvalue == 0) {
            return `
                M${x(Math.min(0, d.prcntvalue))},${y(d.featurename)}
                h0
                a${rx},${ry} 0 0 1 ${rx},${ry}
                v0
                a${rx},${ry} 0 0 1 ${-rx},${ry}
                h0Z
              `;
          } else if(d.prcntvalue > 0) {
            if((Math.abs(x(d.prcntvalue) - x(0))-rx) < 0) {
              return `
                M${x(Math.min(0, d.prcntvalue))},${y(d.featurename)}
                h0
                a${rx},${ry} 0 0 1 ${rx},${ry}
                v0
                a${rx},${ry} 0 0 1 ${-rx},${ry}
                h0Z
              `;
            } else {
            return `
                M${x(Math.min(0, d.prcntvalue))},${y(d.featurename)}
                h${(Math.abs(x(d.prcntvalue) - x(0))-rx)}
                a${rx},${ry} 0 0 1 ${rx},${ry}
                v0
                a${rx},${ry} 0 0 1 ${-rx},${ry}
                h${-(Math.abs(x(d.prcntvalue) - x(0))-rx)}Z
              `;
            }
          } else {
            if((-(Math.abs(x(d.prcntvalue) - x(0))-rx)) > 0) {
              return `
                M${x(Math.max(0, d.prcntvalue))},${y(d.featurename)}
                h0
                a${rx},${ry} 1 0 0 ${-rx},${ry}
                v0
                a${rx},${ry} 1 0 0 ${rx},${ry}
                h0Z
              `;
            } else {
              return `
                M${x(Math.max(0, d.prcntvalue))},${y(d.featurename)}
                h${-(Math.abs(x(d.prcntvalue) - x(0))-rx)}
                a${rx},${ry} 1 0 0 ${-rx},${ry}
                v0
                a${rx},${ry} 1 0 0 ${rx},${ry}
                h${(Math.abs(x(d.prcntvalue) - x(0))-rx)}Z
              `;
            }
            
          }
          })
        .attr('fill', function (d,i) {
          if (d.featurename.includes('Cluster')) {
            return _this.setClusterBarColor(d.featurename);
          } else {
          //  return d['color'];
            return sequentialScale(i*10);
          }
        });


      // add a value label to the right of each bar
      svg.selectAll('.bar').data(this.data)
      .enter()
      .append('text')
      .attr('class', 'barvalue')
      // y position of the label is halfway down the bar
      .attr('y', function (d) {
        return y(d.featurename) ;
      })
      // x position is 3 pixels to the right of the bar
      .attr('x', function (d) {
        return x(Math.min(0, d.prcntvalue)) + 3;
      })
      // .attr('font-size', 12)
      // .attr('font-family', 'sans-serif')
      // .text(function (d) {
      //   let value = d.prcntvalue.toString()
      //   if ( value.length > 5) {
      //   value = value.substring(0,6);
      //   }
      //   return value;
      // });  

      svg.append('g')
        .attr('class','x-Axis')
        .attr('color', 'grey')
        .attr('transform', 'translate(0,' + height + ')')
        .call(d3.axisBottom(x));


      

          
      svg.selectAll('text')
        .style('text-anchor', 'end')
        .attr('transform', 'rotate(-65)');

      svg.selectAll('.barvalue')
        .style('text-anchor', 'start !important')
        .attr('transform', 'rotate(0) !important');

      let maxw = 0;
      // add the y Axis
      svg.append('g')
        .attr('class','y-axis-text')
        .attr('color', 'grey')
        .attr('transform', 'translate(' + x(0) + ',0)')
        .call(d3.axisLeft(y).tickSizeOuter(10))
        .selectAll('text').each(function () {
          if (this.getBBox().width > maxw) {
            maxw = this.getBBox().width;
          }
        });

      svg.selectAll('.rect-path')
        .on('mouseover', function (d) {
          div.transition()
            .duration(400)
            .style('opacity', .9);
          div.html('<div class=text-align><b>Feature : </b>' + d.featurename + '</div><div class=text-align><b>Weight : </b>' +
            d.prcntvalue + '</div>')
            .style('left', _this.setLeft(d3.event))
            .style('top', _this.setTop(d3.event));
        })
        .on('mouseout', function (d) {
          div.transition()
            .duration(500)
            .style('opacity', 0);
        });

        const wrapText = _this.wrapWordEllipse;
        svg.selectAll('.y-axis-text text')
          .call(wrapText, 30)
          .on('mouseover', function (d) {
            div.transition()
              .duration(400)
              .style('opacity', .9);
            div.html(d)
              .style('left', _this.setLeftfeature(d3.event))
              // .style('background-color', 'transparent')
              .style('top', _this.setTopfeature(d3.event));
          })
          .on('mouseout', function (d) {
            div.transition()
              .duration(500)
              .style('opacity', 0);
          });

        d3.selectAll('g.x-Axis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.7');
        d3.selectAll('g.y-Axis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.7');
        d3.selectAll('g.x-Axis').selectAll('.tick line').remove();
        d3.selectAll('g.y-Axis').selectAll('.tick line').remove();
        d3.selectAll('g.xaxis-grid').select('path').remove();
        d3.selectAll('g.yaxis-grid').select('path').remove();
    }
  }


  wrapWordEllipse(label, width) {
    label.each(function () {
      // const self = d3.select(this);
      // let textLength = label.length;
      // let text = self.text();
      let text = d3.select(this);
       text = text._groups[0][0];
       if ( text.textContent.length > 5) {
       text.textContent = text.textContent.substring(0, 5) + '..';
       }
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

  setLeftfeature(event) {
    let left;
    left = (event.layerX + 10) + 'px';
    return left;
  }
  setTopfeature(event) {
    let top;
    top = (event.layerY + 20)  + 'px';
    return top;
  }

  setClusterBarColor(clusterName: string) {
    return DEFAULT_CLUSTER_COLORS[clusterName];
  }
}
