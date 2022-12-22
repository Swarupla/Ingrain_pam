import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MyclientListComponent } from './myclient-list.component';

describe('MyclientListComponent', () => {
  let component: MyclientListComponent;
  let fixture: ComponentFixture<MyclientListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MyclientListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MyclientListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
