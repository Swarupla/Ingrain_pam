import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PublishCascadeModelsComponent } from './publish-cascade-models.component';

describe('PublishCascadeModelsComponent', () => {
  let component: PublishCascadeModelsComponent;
  let fixture: ComponentFixture<PublishCascadeModelsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PublishCascadeModelsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PublishCascadeModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
